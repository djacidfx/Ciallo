using System;
using Ciallo.Data;
using Ciallo.Rendering;
using Ciallo.Widget;
using Massive;
using R3;
using Stateless;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State, SelectTool.Event>.TriggerWithParameters<Entity>;

public partial class SelectTool : CommonToolBase
{
    public readonly StrokeSelectionHintInteractor HintInteractor = new();
    public readonly ImageEditHover ImageEditHover = new();

    private HoverBase _hover;
    public override HoverBase HoveringInteractor => _hover;

    public readonly StateMachine<State, Event> ToolStateMachine = new(State.Inactive);

    public new enum State
    {
        Active,
        Inactive,
        EditingImageLayer,
    }

    public new enum Event
    {
        SwitchWorkingLayer,
        Activate,
        Deactivate,
    }

    private readonly EntityParameterEvent _etWorkingLayerSwitch;
    
    public SelectTool()
    {
        ToolStateMachine.OnUnhandledTrigger((_, _) => { });
        _etWorkingLayerSwitch = ToolStateMachine.SetTriggerParameters<Entity>(Event.SwitchWorkingLayer);

        ToolStateMachine.Configure(State.Inactive)
            .Permit(Event.Activate, State.Active);
        ToolStateMachine.Configure(State.Active)
            .Permit(Event.Deactivate, State.Inactive)
            .PermitDynamic(_etWorkingLayerSwitch, e =>
            {
                if (e.IsNull()) return State.Active;
                if (e.Has<ImageLayerSetting>()) return State.EditingImageLayer;
                return State.Active;
            });
        Entity currLayerE = new();
        ToolStateMachine.Configure(State.EditingImageLayer)
            .SubstateOf(State.Active)
            .OnEntryFrom(_etWorkingLayerSwitch, (layerE, _) =>
            {
                layerE.Get<ImageLayerOverlay>().Visible = true;
                currLayerE = layerE;
                _hover = ImageEditHover;
            })
            .OnExit(() =>
            {
                base.OnDeactivate();
                _hover = null;
                currLayerE.Get<ImageLayerOverlay>().Visible = false;
            });
    }

    public override void DrawProperty(PropertyContainer container)
    {
    }

    private IDisposable _subsToWorkingLayer;

    public override void OnActivate()
    {
        ToolStateMachine.Fire(Event.Activate);
        _subsToWorkingLayer = Document.Get<SelectionManager>().WorkingLayer
            .Subscribe(e => ToolStateMachine.Fire(_etWorkingLayerSwitch, e));
    }

    public override void OnDeactivate()
    {
        _subsToWorkingLayer?.Dispose();
        ToolStateMachine.Fire(Event.Deactivate);
        base.OnDeactivate();
    }
}