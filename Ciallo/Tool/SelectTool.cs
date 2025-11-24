using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.Widget;
using Frent;
using Godot;
using ObservableCollections;
using R3;
using Stateless;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State, SelectTool.Event>.TriggerWithParameters<Entity>;

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
                HoverInteractor = ImageTransformHover;
                LeftInteractor = ImageTransformInteractor;
            })
            .OnExit(() =>
            {
                LeftInteractor = null;
                HoverInteractor = null;
            });

        // Polyline layer
        ToolStateMachine.Configure(State.EditingPolylineLayer).SubstateOf(State.Active)
            .OnEntryFrom(_etSwitchWorkingLayer, (layerE, _) =>
            {
                LeftInteractor = PolylineTransformInteractor;
                HoverInteractor = PolylineTransformHover;
                RightInteractor = PolylineDeleteInteractor;
            })
            .OnExit(() =>
            {
                HoverInteractor = null;
                LeftInteractor = null;
                RightInteractor = null;
                Document.Get<SelectionManager>().SelectedPolylines.Clear();
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
            var cmd = new EmptyCommand();
            foreach (var polylineE in selectionManager.SelectedPolylines)
            {
                if (polylineE.Has<StrokeSetting>())
                    cmd.Combine(new DeleteStrokeCmd(polylineE));
                else
                    cmd.Combine(new DeleteFilledPolygonCmd(polylineE));
            }
            cmd.Commit();
        }
        base.OnKey(key);
    }

    public ReactiveProperty<float> SimplificationRatio = new(0.25f);

    public override void DrawProperty(PropertyContainer container)
    {
        var selectionManager = Document.Get<SelectionManager>();
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
            var cmd = new EmptyCommand();
            foreach (var polylineE in selectionManager.SelectedPolylines)
            {
                var geom = polylineE.Get<PolylineGeometry>();
                if (geom.Positions.Count < 4) continue;
                geom.Positions.SimplifyH(SimplificationRatio.Value, out var indices);
                var newGeom = geom.Index(indices);
                cmd.Combine(new SetPolylineGeometryCmd(polylineE, newGeom));
            }
            cmd.Commit();
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