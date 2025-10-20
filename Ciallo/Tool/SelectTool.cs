using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;
using Stateless;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State, SelectTool.Event>.TriggerWithParameters<Entity>;

public partial class SelectTool : CommonToolBase
{
    public readonly PolylineHover PolylineHover = new();
    public readonly PolylineTransformInteractor PolylineTransformInteractor;
    public readonly ImageEditHover ImageEditHover = new();
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
        PolylineTransformInteractor = new(PolylineHover);
        ImageTransformInteractor = new(ImageEditHover);

        ToolStateMachine.OnUnhandledTrigger((_, _) => { });
        _etSwitchWorkingLayer = ToolStateMachine.SetTriggerParameters<Entity>(Event.SwitchWorkingLayer);

        ToolStateMachine.Configure(State.Inactive)
            .Permit(Event.Activate, State.Active);
        ToolStateMachine.Configure(State.Active)
            .Permit(Event.Deactivate, State.Inactive)
            .PermitDynamic(_etSwitchWorkingLayer, e =>
            {
                if (e.IsNull) return State.Active;
                if (e.Has<ImageLayerSetting>()) return State.EditingImageLayer;
                if (e.Has<PolylineLayerSetting>()) return State.EditingPolylineLayer;
                return State.Active;
            });

        // Image layer
        ToolStateMachine.Configure(State.EditingImageLayer).SubstateOf(State.Active)
            .OnEntryFrom(_etSwitchWorkingLayer, (layerE, _) =>
            {
                HoverInteractor = ImageEditHover;
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
                HoverInteractor = PolylineHover;
            })
            .OnExit(() =>
            {
                HoverInteractor = null;
                LeftInteractor = null;
                Document.Get<SelectionManager>().SelectedPolylines.Clear();
            });
    }

    public override void OnKey(InputEventKey key)
    {
        if (AppActions.CancelInteraction.IsJustPressed)
        {
            Document.Get<SelectionManager>().SelectedPolylines.Clear();
        }
        base.OnKey(key);
    }

    public override void DrawProperty(PropertyContainer container)
    {
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
        if (newLayerE.IsNull) return false;
        ToolStateMachine.Fire(_etSwitchWorkingLayer, newLayerE);
        bool isPolyline = newLayerE.Has<PolylineLayerSetting>();
        bool isImage = newLayerE.Has<ImageLayerSetting>();

        return isPolyline || isImage;
    }
}