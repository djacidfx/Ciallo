using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiBinding;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Select)]
public class PolylineTransformTool : StateMachineToolBase
{
    public readonly ShapeTransformHover Hover = new();
    public readonly PolylineTransformInteractor Transform = new();
    public readonly RectSelectShapeInteractor Select = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitDynamic(Press(MouseButton.Left), () =>
            {
                if (Hover.CanTransform)
                    return Transform;
                return Select;
            });

        Configure(Transform)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);

        Configure(Select)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public readonly ReactiveProperty<float> SimplificationRatio = new(0.25f);

    public override void DrawProperty(PropertyContainer container)
    {
        var selectionManager = Document.Get<SelectionManager>();
        var selectionButtonGroup = PropertyContainer.CreateHContainer().AddToChildOf(container);
        var selectAllButton = PropertyContainer.CreateButton("Select all").AddToChildOf(selectionButtonGroup);
        selectAllButton.Pressed += () =>
        {
            var layerE = selectionManager.WorkingLayer.Value;
            if (layerE.IsDyingOrDead) return;
            selectionManager.SelectedShapes.Clear();
            selectionManager.SelectedShapes.AddRange(layerE.Get<LayerTreeNode>().Children);
            Machine.Fire(Trigger.Refresh);
        };
        var deselectAllButton = PropertyContainer.CreateButton("Deselect").AddToChildOf(selectionButtonGroup);
        deselectAllButton.Pressed += () =>
        {
            selectionManager.SelectedShapes.Clear();
            Machine.Fire(Trigger.Refresh);
        };

        var polylineEditBox = PropertyContainer.CreateBox().AddToChildOf(container)
            .VisibleIf(selectionManager.SelectedShapes.ObserveCountChanged().Prepend(0), count => count > 0);

        var simplificationRatioEdit = new SpinSlider()
        {
            MinValue = 0.1,
            MaxValue = 0.5,
        };
        simplificationRatioEdit.BindNumber(SimplificationRatio);
        PropertyContainer.CreatePropertyControl("Simplification ratio", simplificationRatioEdit).AddToChildOf(polylineEditBox);

        var simplifyButton = PropertyContainer.CreateButton("Simplify").AddToChildOf(polylineEditBox);
        simplifyButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Positions.Count < 4) continue;
                geom.Positions.SimplifyCurvatureDistance(SimplificationRatio.Value, out var indices);
                var newGeom = geom.Index(indices);
                builder.SetTarget(polylineE).SetPolylineGeometry(newGeom);
            }
            builder.Commit();
        };

        var smoothSubdivideButton = PropertyContainer.CreateButton("Smooth subdivide").AddToChildOf(polylineEditBox);
        smoothSubdivideButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Positions.Count < 2) continue;
                List<float> polyTs = new() { Capacity = geom.Positions.Count * 2 - 1 };
                for (int i = 0; i < geom.Positions.Count - 1; i++)
                {
                    polyTs.Add(i);
                    polyTs.Add(i + 0.5f);
                }
                polyTs.Add(geom.Positions.Count - 1);
                var newGeom = geom.CatmullRomSample(polyTs);
                builder.SetTarget(polylineE).SetPolylineGeometry(newGeom);
            }
            builder.Commit();
        };

        var linearSubdivideButton = PropertyContainer.CreateButton("Linear subdivide").AddToChildOf(polylineEditBox);
        linearSubdivideButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Positions.Count < 2) continue;
                List<float> polyTs = new() { Capacity = geom.Positions.Count * 2 - 1 };
                for (int i = 0; i < geom.Positions.Count - 1; i++)
                {
                    polyTs.Add(i);
                    polyTs.Add(i + 0.5f);
                }
                polyTs.Add(geom.Positions.Count - 1);
                var newGeom = geom.Sample(polyTs);
                builder.SetTarget(polylineE).SetPolylineGeometry(newGeom);
            }
            builder.Commit();
        };

        var smoothButton = PropertyContainer.CreateButton("Smooth").AddToChildOf(polylineEditBox);
        smoothButton.Pressed += () =>
        {
            var builder = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedShapes)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Positions.Count < 3) continue;

                // Apply Laplacian smoothing only to positions.
                const int iterations = 1;
                const float lambda = 0.5f;
                var smoothedPositions = geom.Positions.SmoothLaplacian(iterations, lambda);

                var newGeom = new PolylineGeometry
                {
                    Positions = smoothedPositions,
                    Radii = geom.Radii,
                    Pressures = geom.Pressures,
                    Tilts = geom.Tilts,
                };

                builder.SetTarget(polylineE).SetPolylineGeometry(newGeom);
            }
            builder.Commit();
        };
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        return !e.IsDyingOrDead && e.Has<ShapeLayerSetting>();
    }

    public override void OnActivated()
    {
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    public override void OnDeactivated()
    {
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}