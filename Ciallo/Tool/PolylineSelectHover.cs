using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Tool;

public class PolylineSelectHover : InteractiveSessionBase
{
    public Entity HoveredShape;
    public Body RotationBody;
    public Body[] CornerBodies = [];
    public bool CanTransform
    {
        get
        {
            bool shapeHovered = !HoveredShape.IsNull;
            bool rotationDotHovered = RotationBody?.IsHovered == true;
            bool cornerDotsHovered = CornerBodies.Any(a => a.IsHovered);
            return shapeHovered || rotationDotHovered || cornerDotsHovered;
        }
    }

    private IDisposable _hoverSub;
    private TransformOverlayBox _transformBox;
    private List<Entity> _shapeEs;
    private readonly List<Node2D> _wireframes = [];

    public readonly ReactiveProperty<float> SimplificationRatio = new(0.25f);

    public override void Start(CursorButtonData data)
    {
        var selectionManager = Document.Get<SelectionManager>();
        var worldBody = Document.Get<WorldBody>();
        var layerBody = WorkingLayer.Get<BodyHolder>();

        // Enable cursor detections on shapes of working layer
        worldBody.EnableHoverDetection = true;
        worldBody.CursorWorldPosition = data.WorldPosition;

        layerBody.SetAreaCursor(Control.CursorShape.Move);

        // hover hinter
        _hoverSub = Document.Get<WorldBody>().HoveringBody.Subscribe(body =>
        {
            if (!HoveredShape.IsDyingOrDead) HoveredShape.Get<PolylineWireframe>().SetVisible(false);
            if (body == null)
            {
                HoveredShape = Entity.Null;
                return;
            }
            if (!body.SelfEntity.IsDyingOrDead)
                body.SelfEntity.Get<PolylineWireframe>().SetVisible(true);
            HoveredShape = body.SelfEntity;
        });

        // Polyline transform
        if (selectionManager.SelectedShapes.Count > 0)
        {
            var worldOverlay = Document.Get<WorldOverlay>();

            _shapeEs = [..selectionManager.SelectedShapes];

            // transform box
            Rect2 rect = default;
            foreach (var (i, e) in _shapeEs.Index())
            {
                var wire = (Node2D)e.Get<PolylineWireframe>().Duplicate(0); // 0 means avoid duplicating script. Script duplication call constructor.
                worldOverlay.AddChild(wire);
                wire.Visible = true;
                _wireframes.Add(wire);

                // transform box overlay
                var bound = e.Get<PolylineGeometry>().Positions.Value.GetBoundingBox();
                rect = i == 0 ? bound : rect.Merge(bound);
            }
            if (!rect.IsEqualApprox(default) && !rect.Size.IsZeroApprox())
            {
                _transformBox = new TransformOverlayBox(rect.Size, rect.GetCenter());
                worldOverlay.AddChild(_transformBox);

                // transform cursor bodies
                Body[] bodies = worldBody.CreateAddTransformAreas(rect.Size, rect.GetCenter());
                RotationBody = bodies[0];
                bodies[1].QueueFree();
                CornerBodies = bodies[2..6];
            }
        }
    }

    public override void Moving(CursorMotionData data)
    {
        Document.Get<WorldBody>().CursorWorldPosition = data.WorldPosition;
    }

    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        _hoverSub.Dispose();

        // cursor bodies
        RotationBody?.QueueFree();
        RotationBody = null;
        Array.ForEach(CornerBodies, b => b.QueueFree());
        CornerBodies = [];

        WorkingLayer.Get<BodyHolder>().SetAreaCursor(Control.CursorShape.Arrow);
        Document.Get<WorldBody>().EnableHoverDetection = false;

        // overlays
        if (!HoveredShape.IsDyingOrDead)
            HoveredShape.Get<PolylineWireframe>().SetVisible(false);
        _wireframes.ForEach(node => node.QueueFree());
        _wireframes.Clear();
        _transformBox?.QueueFree();
        _transformBox = null;

        HoveredShape = Entity.Null;
    }

    public void Restart(CursorButtonData data = default)
    {
        Cancel();
        Start(data);
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data)
    {
        if (AppActions.CancelInteraction.IsJustPressed)
        {
            Document.Get<SelectionManager>().SelectedShapes.Clear();
            Restart(data);
            return true;
        }

        if (AppActions.Delete.IsJustPressed)
        {
            var cmd = new CommandBuilder();
            foreach (var e in Document.Get<SelectionManager>().SelectedShapes)
            {
                cmd.SetTarget(e).RemoveFromLayerTree().DeleteShape();
            }
            cmd.Commit();
            Restart(data);
            return true;
        }

        return false;
    }

    public override void DrawProperty(PropertyContainer container)
    {
        var selectionManager = Document.Get<SelectionManager>();
        var selectionButtonGroup = container.CreateHContainer().AddToChildOf(container);
        var selectAllButton = container.CreateButton("Select all").AddToChildOf(selectionButtonGroup);
        selectAllButton.Pressed += () =>
        {
            var layerE = selectionManager.WorkingLayer.Value;
            if (layerE.IsDyingOrDead) return;
            selectionManager.SelectedShapes.Clear();
            selectionManager.SelectedShapes.AddRange(layerE.Get<LayerTreeNode>().Children);
            Restart();
        };
        var deselectAllButton = container.CreateButton("Deselect").AddToChildOf(selectionButtonGroup);
        deselectAllButton.Pressed += () =>
        {
            selectionManager.SelectedShapes.Clear();
            Restart();
        };

        var polylineEditBox = container.CreateBox().AddToChildOf(container)
            .VisibleIf(selectionManager.SelectedShapes.ObserveCountChanged().Prepend(0), count => count > 0);

        var simplificationRatioEdit = new SpinSlider()
        {
            MinValue = 0.1,
            MaxValue = 0.5,
        };
        simplificationRatioEdit.BindNumber(SimplificationRatio);
        container.CreatePropertyBox("Simplification ratio", simplificationRatioEdit).AddToChildOf(polylineEditBox);

        var simplifyButton = container.CreateButton("Simplify").AddToChildOf(polylineEditBox);
        simplifyButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Length < 4) continue;
                geom.Positions.Value.SimplifyCurvatureDistance(SimplificationRatio.Value, out var indices);

                var positions = ImmutableArray.CreateBuilder<Vector2>(indices.Count);
                var radii = ImmutableArray.CreateBuilder<float>(indices.Count);
                var pressures = ImmutableArray.CreateBuilder<float>(indices.Count);
                var tilts = ImmutableArray.CreateBuilder<Vector2>(indices.Count);

                foreach (var idx in indices)
                {
                    positions.Add(geom.Positions.Value[idx]);
                    radii.Add(geom.Radii.Value[idx]);
                    pressures.Add(geom.Pressures.Value[idx]);
                    tilts.Add(geom.Tilts.Value[idx]);
                }
                builder.SetTarget(polylineE).SetPolylineGeometry(
                    positions.MoveToImmutable(),
                    radii.MoveToImmutable(),
                    pressures.MoveToImmutable(),
                    tilts.MoveToImmutable()
                );
            }
            builder.Commit();
        };

        var smoothSubdivideButton = container.CreateButton("Smooth subdivide").AddToChildOf(polylineEditBox);
        smoothSubdivideButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Length < 2) continue;

                var resultLength = geom.Length * 2 - 1;
                var positions = ImmutableArray.CreateBuilder<Vector2>(resultLength);
                var radii = ImmutableArray.CreateBuilder<float>(resultLength);
                var pressures = ImmutableArray.CreateBuilder<float>(resultLength);
                var tilts = ImmutableArray.CreateBuilder<Vector2>(resultLength);

                var oldPositions = geom.Positions.Value;
                var oldRadii = geom.Radii.Value;
                var oldPressures = geom.Pressures.Value;
                var oldTilts = geom.Tilts.Value;

                for (int i = 0; i < geom.Length; i++)
                {
                    positions.Add(oldPositions[i]);
                    radii.Add(oldRadii[i]);
                    pressures.Add(oldPressures[i]);
                    tilts.Add(oldTilts[i]);

                    if (i == geom.Length - 1) continue;
                    var idx1 = i;
                    int idx0 = idx1 == 0 ? idx1 : idx1 - 1;
                    int idx2 = idx1 >= geom.Length - 1 ? idx1 : idx1 + 1;
                    int idx3 = idx2 >= geom.Length - 1 ? idx2 : idx2 + 1;

                    float t = 0.5f;
                    var p = oldPositions[idx0].CatmullRomInterpolation(oldPositions[idx1], oldPositions[idx2], oldPositions[idx3], t);
                    var r = oldRadii[idx0].CatmullRomInterpolation(oldRadii[idx1], oldRadii[idx2], oldRadii[idx3], t);
                    var pp = oldPressures[idx0].CatmullRomInterpolation(oldPressures[idx1], oldPressures[idx2], oldPressures[idx3], t);
                    var tilt = oldTilts[idx0].CatmullRomInterpolation(oldTilts[idx1], oldTilts[idx2], oldTilts[idx3], t);
                    positions.Add(p);
                    radii.Add(r);
                    pressures.Add(pp);
                    tilts.Add(tilt);
                }
                builder.SetTarget(polylineE).SetPolylineGeometry(
                    positions.MoveToImmutable(),
                    radii.MoveToImmutable(),
                    pressures.MoveToImmutable(),
                    tilts.MoveToImmutable()
                );
            }
            builder.Commit();
        };

        var linearSubdivideButton = container.CreateButton("Linear subdivide").AddToChildOf(polylineEditBox);
        linearSubdivideButton.Pressed += () =>
        {
            var cmd = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Length < 2) continue;
                List<float> polyTs = new() { Capacity = geom.Length * 2 - 1 };
                for (int i = 0; i < geom.Length - 1; i++)
                {
                    polyTs.Add(i);
                    polyTs.Add(i + 0.5f);
                }
                polyTs.Add(geom.Length - 1);

                var positions = ImmutableArray.CreateBuilder<Vector2>(polyTs.Count);
                var radii = ImmutableArray.CreateBuilder<float>(polyTs.Count);
                var pressures = ImmutableArray.CreateBuilder<float>(polyTs.Count);
                var tilts = ImmutableArray.CreateBuilder<Vector2>(polyTs.Count);

                foreach (var polyT in polyTs)
                {
                    var (idx, t) = polyT.ResolvePolyT();
                    int nIdx = int.Min(idx + 1, geom.Count - 1);
                    positions.Add(geom.Positions.Value[idx].Lerp(geom.Positions.Value[nIdx], t));
                    radii.Add(float.Lerp(geom.Radii.Value[idx], geom.Radii.Value[nIdx], t));
                    pressures.Add(float.Lerp(geom.Pressures.Value[idx], geom.Pressures.Value[nIdx], t));
                    tilts.Add(geom.Tilts.Value[idx].Lerp(geom.Tilts.Value[nIdx], t));
                }

                cmd.SetTarget(polylineE).SetPolylineGeometry(
                    positions.MoveToImmutable(),
                    radii.MoveToImmutable(),
                    pressures.MoveToImmutable(),
                    tilts.MoveToImmutable()
                );
            }
            cmd.Commit();
        };

        var smoothButton = container.CreateButton("Smooth").AddToChildOf(polylineEditBox);
        smoothButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Length < 3) continue;

                // Apply Laplacian smoothing only to positions.
                const int iterations = 1;
                const float lambda = 0.5f;
                var smoothedPositions = geom.Positions.Value.SmoothLaplacian(iterations, lambda);
                builder.SetTarget(polylineE).SetPolylineGeometry([..smoothedPositions]); // copy but fine
            }
            builder.Commit();
        };
    }
}