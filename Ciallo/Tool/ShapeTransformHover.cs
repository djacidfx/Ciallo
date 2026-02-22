using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class ShapeTransformHover : InteractiveSessionBase
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

    public override void Start(CursorButtonData data)
    {
        var selectionManager = Document.Get<SelectionManager>();
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
                var geom = e.Get<PolylineGeometry>();
                var bound = geom.Positions.Value.GetBoundingBox();
                rect = i == 0 ? bound : rect.Merge(bound);
            }
            if (!rect.IsEqualApprox(default))
            {
                _transformBox = new TransformOverlayBox(rect.Size, rect.GetCenter());
                worldOverlay.AddChild(_transformBox);
            }

            // transform cursor bodies
            var worldBody = Document.Get<WorldBody>();
            Body[] bodies = worldBody.CreateAddTransformAreas(rect.Size, rect.GetCenter());
            RotationBody = bodies[0];
            bodies[1].QueueFree();
            CornerBodies = bodies[2..6];
        }

        // Enable cursor detections on shapes of working layer
        WorkingLayer.Get<BodyHolder>().SetAreaCursor(Control.CursorShape.Move);

        // hover hinter
        _hoverSub = Document.Get<WorldBody>().HoveringBody.Skip(1).Subscribe(body =>
        {
            if (!HoveredShape.IsDyingOrDead) HoveredShape.Get<PolylineWireframe>().SetVisible(false);
            if (body == null)
            {
                HoveredShape = Entity.Null;
                return;
            }
            HoveredShape = body.SelfEntity;
            if (!HoveredShape.IsNull) HoveredShape.Get<PolylineWireframe>().SetVisible(true);
        });
    }
    public override void Interacting(CursorMotionData data) { }
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

        // overlays
        if (!HoveredShape.IsDyingOrDead) HoveredShape.Get<PolylineWireframe>().SetVisible(false);
        _wireframes.ForEach(node => node.QueueFree());
        _wireframes.Clear();
        _transformBox?.QueueFree();
        _transformBox = null;

        HoveredShape = Entity.Null;
    }

    public void Restart(CursorButtonData data)
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
}