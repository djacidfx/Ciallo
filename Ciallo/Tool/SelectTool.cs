using System;
using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiBinding;
using Ciallo.Misc;
using Ciallo.Widget;
using Frent;
using Godot;
using ObservableCollections;
using R3;
using Stateless;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State, SelectTool.Event>.TriggerWithParameters<Entity>;

// This class is very messy because it handles multiple types of layers.
// Need a better tool mechanics
public partial class SelectTool : CommonToolBase
{
    public readonly PolylineTransformHover PolylineTransformHover = new();
    public readonly PolylineTransformInteractor PolylineTransformInteractor;
    public readonly PolylineDeleteInteractor PolylineDeleteInteractor;
    public readonly ImageTransformHover ImageTransformHover = new();
    public readonly ImageTransformInteractor ImageTransformInteractor;

    public readonly StateMachine<State, Event> ToolStateMachine = new(State.Inactive);

    public new enum State
    {
        Inactive,
        Active,

        EditingImageLayer,
        EditingPolylineLayer,
    }

    public new enum Event
    {
        SwitchWorkingLayer,
        Activate,
        Deactivate,
    }

    private readonly EntityParameterEvent _etSwitchWorkingLayer;
    private Entity _currentLayerE;

    public SelectTool()
    {
        PolylineTransformInteractor = new(PolylineTransformHover);
        PolylineDeleteInteractor = new(PolylineTransformHover);
        ImageTransformInteractor = new(ImageTransformHover);

        ToolStateMachine.OnUnhandledTrigger((_, _) => { });
        _etSwitchWorkingLayer = ToolStateMachine.SetTriggerParameters<Entity>(Event.SwitchWorkingLayer);

        ToolStateMachine.Configure(State.Inactive)
            .Permit(Event.Activate, State.Active);
        ToolStateMachine.Configure(State.Active)
            .Permit(Event.Deactivate, State.Inactive)
            .PermitDynamic(_etSwitchWorkingLayer, e =>
            {
                if (e.IsDeletedOrNull()) return State.Active;
                if (e.Has<ImageLayerSetting>()) return State.EditingImageLayer;
                if (e.Has<PolylineLayerSetting>()) return State.EditingPolylineLayer;
                return State.Active;
            });

        // Image layer
        ToolStateMachine.Configure(State.EditingImageLayer).SubstateOf(State.Active)
            .OnEntryFrom(_etSwitchWorkingLayer, (layerE, _) =>
            {
                _currentLayerE = layerE;
                HoverInteractor = ImageTransformHover;
                LeftInteractor = ImageTransformInteractor;
            })
            .OnExit(() =>
            {
                LeftInteractor = null;
                HoverInteractor = null;
                _currentLayerE = Entity.Null;
            });

        // Polyline layer
        ToolStateMachine.Configure(State.EditingPolylineLayer).SubstateOf(State.Active)
            .OnEntryFrom(_etSwitchWorkingLayer, layerE =>
            {
                _currentLayerE = layerE;
                layerE.Get<PolylineAreaHolder>().ProcessMode = ProcessModeEnum.Inherit;
                LeftInteractor = PolylineTransformInteractor;
                HoverInteractor = PolylineTransformHover;
                RightInteractor = PolylineDeleteInteractor;
            })
            .OnExit(() =>
            {
                _currentLayerE.Get<PolylineAreaHolder>().ProcessMode = ProcessModeEnum.Disabled;
                HoverInteractor = null;
                LeftInteractor = null;
                RightInteractor = null;
                Document.Get<SelectionManager>().SelectedPolylines.Clear();
                _currentLayerE = Entity.Null;
            });
    }

    public override void OnKey(InputEventKey key)
    {
        if (AppActions.CancelInteraction.IsJustPressed)
        {
            Document.Get<SelectionManager>().SelectedPolylines.Clear();
        }

        if (AppActions.Delete.IsJustPressed)
        {
            var selectionManager = Document.Get<SelectionManager>();
            if (selectionManager.SelectedPolylines.Count == 0) return;
            var cmd = new CommandBuilder(Entity.Null);
            foreach (var polylineE in selectionManager.SelectedPolylines)
            {
                if (polylineE.Has<StrokeSetting>())
                    cmd.SetTarget(polylineE).DeleteStroke();
                else
                    cmd.SetTarget(polylineE).DeleteFilledPolygon();
            }
            selectionManager.SelectedPolylines.Clear();
            cmd.Commit();
        }
        base.OnKey(key);
    }

    public ReactiveProperty<float> SimplificationRatio = new(0.25f);

    public override void DrawProperty(PropertyContainer container)
    {
        var selectionManager = Document.Get<SelectionManager>();
        var selectionButtonGroup = PropertyContainer.CreateHContainer().AddToChildOf(container)
            .VisibleIf(selectionManager.WorkingLayer, e => !e.IsNull && e.Has<PolylineLayerSetting>());
        var selectAllButton = PropertyContainer.CreateButton("Select all").AddToChildOf(selectionButtonGroup);
        selectAllButton.Pressed += () =>
        {
            var layerE = selectionManager.WorkingLayer.Value;
            if (layerE.IsDeletedOrNull()) return;
            selectionManager.SelectedPolylines.Clear();
            selectionManager.SelectedPolylines.AddRange(layerE.Get<LayerTreeNode>().Children);
            FireRefreshHover();
        };
        var deselectAllButton = PropertyContainer.CreateButton("Deselect").AddToChildOf(selectionButtonGroup);
        deselectAllButton.Pressed += () =>
        {
            selectionManager.SelectedPolylines.Clear();
            FireRefreshHover();
        };

        var polylineEditBox = PropertyContainer.CreateBox().AddToChildOf(container)
            .VisibleIf(selectionManager.SelectedPolylines.ObserveCountChanged().Prepend(0), count => count > 0);

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
            foreach (var polylineE in selectionManager.SelectedPolylines)
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
            foreach (var polylineE in selectionManager.SelectedPolylines)
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
            foreach (var polylineE in selectionManager.SelectedPolylines)
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
            foreach (var polylineE in selectionManager.SelectedPolylines)
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

    private IDisposable _subToWorkingLayer;
    public override void OnActivate()
    {
        ToolStateMachine.Fire(Event.Activate);
        base.OnActivate();
        _subToWorkingLayer = Document.Get<SelectionManager>().WorkingLayer
            .Subscribe(e => ToolStateMachine.Fire(_etSwitchWorkingLayer, e));
    }

    public override void OnDeactivate()
    {
        _subToWorkingLayer.Dispose();
        base.OnDeactivate();
        ToolStateMachine.Fire(Event.Deactivate);
    }

    public override bool OnSwitchLayer(Entity newLayerE)
    {
        if (newLayerE.IsDeletedOrNull()) return false;
        ToolStateMachine.Fire(_etSwitchWorkingLayer, newLayerE);
        bool isPolyline = newLayerE.Has<PolylineLayerSetting>();
        bool isImage = newLayerE.Has<ImageLayerSetting>();

        return isPolyline || isImage;
    }
}