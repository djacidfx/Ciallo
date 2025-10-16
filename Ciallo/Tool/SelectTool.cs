using System;
using Ciallo.Data;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using R3;
using Stateless;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State, SelectTool.Event>.TriggerWithParameters<Entity>;

public partial class SelectTool : CommonToolBase
{
    public readonly PolylineSelectionHintHover PolylineSelectionHover = new();
    public readonly ImageEditHover ImageEditHover = new();
    public readonly ImageTransformInteractor ImageTransformInteractor;

    public readonly StateMachine<State, Event> ToolStateMachine = new(State.Inactive);

    public new enum State
    {
        Active,
        Inactive,

        EditingImageLayer,

        EditingPolylineLayer,
        TransformingPolyline,
    }

    public new enum Event
    {
        SwitchWorkingLayer,
        Activate,
        Deactivate,

        SelectPolyline,
        DeselectPolyline,
    }

    private readonly EntityParameterEvent _etSwitchWorkingLayer;

    public SelectTool()
    {
        ImageTransformInteractor = new(ImageEditHover);

        ToolStateMachine.OnUnhandledTrigger((_, _) => { });
        _etSwitchWorkingLayer = ToolStateMachine.SetTriggerParameters<Entity>(Event.SwitchWorkingLayer);

        ToolStateMachine.Configure(State.Inactive)
            .Permit(Event.Activate, State.Active);
        ToolStateMachine.Configure(State.Active)
            .Permit(Event.Deactivate, State.Inactive)
            .PermitDynamic(_etSwitchWorkingLayer, e =>
            {
                if (e.IsNull()) return State.Active;
                if (e.Has<ImageLayerSetting>()) return State.EditingImageLayer;
                if (e.Has<PolylineLayerSetting>()) return State.EditingPolylineLayer;
                return State.Active;
            });

        Entity currLayerE = new();
        ToolStateMachine.Configure(State.EditingImageLayer).SubstateOf(State.Active)
            .Permit(Event.SelectPolyline, State.TransformingPolyline)
            .OnEntryFrom(_etSwitchWorkingLayer, (layerE, _) =>
            {
                layerE.Get<ImageLayerOverlay>().Visible = true;
                currLayerE = layerE;
                HoverInteractor = ImageEditHover;
                LeftInteractor = ImageTransformInteractor;
            })
            .OnExit(() =>
            {
                LeftInteractor = null;
                HoverInteractor = null;
                currLayerE.Get<ImageLayerOverlay>().Visible = false;
            });

        ToolStateMachine.Configure(State.EditingPolylineLayer).SubstateOf(State.Active)
            .Permit(Event.SelectPolyline, State.TransformingPolyline)
            .OnEntryFrom(_etSwitchWorkingLayer, (layerE, _) =>
            {
                currLayerE = layerE;
                HoverInteractor = PolylineSelectionHover;
            })
            .OnExit(() =>
            {
                HoverInteractor = null;
            });
        ToolStateMachine.Configure(State.TransformingPolyline).SubstateOf(State.EditingPolylineLayer)
            .Permit(Event.DeselectPolyline, State.EditingPolylineLayer)
            .OnEntry(() =>
            {
            })
            .OnExit(() =>
            {
            });
    }

    public override void DrawProperty(PropertyContainer container)
    {
    }

    private IDisposable _subToWorkingLayer;
    public override void OnActivate()
    {
        base.OnActivate();
        ToolStateMachine.Fire(Event.Activate);
        _subToWorkingLayer = Document.Get<SelectionManager>().WorkingLayer
            .Subscribe(e => ToolStateMachine.Fire(_etSwitchWorkingLayer, e));
    }

    public override void OnDeactivate()
    {
        _subToWorkingLayer.Dispose();
        ToolStateMachine.Fire(Event.Deactivate);
        base.OnDeactivate();
    }
}