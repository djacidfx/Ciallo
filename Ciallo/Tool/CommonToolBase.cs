using Ciallo.Command;
using Ciallo.NodeControl;
using Godot;
using Stateless;

namespace Ciallo.Tool;

using buttonParameterEvent = StateMachine<CommonToolBase.State,CommonToolBase.Event>.TriggerWithParameters<CursorButtonData>;
using motionParameterEvent = StateMachine<CommonToolBase.State,CommonToolBase.Event>.TriggerWithParameters<CursorMotionData>;

/// <summary>
/// The base class for common tools that need for three states: Hovering, left mouse drag, right mouse drag.
/// (Middle mouse has dedicated usage for canvas navigation.)
/// For more complex tools, we can write state machine code and implement ITool directly.
/// </summary>
public abstract partial class CommonToolBase : ToolButtonBase
{
    public virtual InteractorBase LeftInteractor => null;
    public virtual InteractorBase HoveringInteractor => null;
    public virtual InteractorBase RightInteractor => null;
    
    public enum State
    {
        Idle,
        HoverInteracting,
        LeftInteracting,
        RightInteracting
    }

    public enum Event
    {
        LeftClick,
        LeftRelease,
        RightClick,
        RightRelease,
        Move,
        Cancel
    }

    private readonly StateMachine<State, Event> Machine = new(State.Idle);
    private readonly buttonParameterEvent _etLeftClick;
    private readonly buttonParameterEvent _etLeftRelease;
    private readonly buttonParameterEvent _etRightClick;
    private readonly buttonParameterEvent _etRightRelease;
    private readonly motionParameterEvent _etMove;

    protected CommonToolBase()
    {
        Machine.OnUnhandledTrigger((_, _) => { }); // Do nothing on unhandled trigger
        _etLeftClick = Machine.SetTriggerParameters<CursorButtonData>(Event.LeftClick);
        _etLeftRelease = Machine.SetTriggerParameters<CursorButtonData>(Event.LeftRelease);
        _etRightClick = Machine.SetTriggerParameters<CursorButtonData>(Event.RightClick);
        _etRightRelease = Machine.SetTriggerParameters<CursorButtonData>(Event.RightRelease);
        _etMove = Machine.SetTriggerParameters<CursorMotionData>(Event.Move);

        Machine.Configure(State.Idle)
            .PermitIf(_etLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_etRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .PermitIf(_etMove, State.HoverInteracting, _ => HoveringInteractor?.CanInteract == true);
        Machine.Configure(State.HoverInteracting)
            .PermitIf(_etLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_etRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .Permit(Event.Cancel, State.Idle)
            .OnExit(() => HoveringInteractor.Cancel());
        Machine.Configure(State.LeftInteracting)
            .OnEntryFrom(_etLeftClick, data =>
            {
                BeforeLeftStart();
                LeftInteractor.Start(data);
            }) 
            .Permit(Event.Cancel, State.Idle)
            .PermitIf(_etLeftRelease, State.Idle) //Permit() cannot accept parameterized trigger, annoying
            .PermitIf(_etRightClick, State.Idle) // Cancel left interaction if right click
            .OnExit(t =>
            {
                if(t.Trigger == Event.Cancel) LeftInteractor.Cancel();
                else LeftInteractor.End((CursorButtonData)t.Parameters[0]);
                AfterLeftEnd();
            });
        Machine.Configure(State.RightInteracting)
            .OnEntryFrom(_etRightClick, data =>
            {
                BeforeRightStart();
                RightInteractor.Start(data);
            })
            .Permit(Event.Cancel, State.Idle)
            .PermitIf(_etRightRelease, State.Idle)
            .PermitIf(_etRightClick, State.Idle)
            .OnExit(t =>
            {
                if(t.Trigger == Event.Cancel) RightInteractor.Cancel();
                else RightInteractor.End((CursorButtonData)t.Parameters[0]);
                AfterRightEnd();
            });
    }

    public virtual void BeforeLeftStart()
    {

    }
    
    public virtual void AfterLeftEnd()
    {
        
    }
    
    public virtual void BeforeRightStart()
    {
        
    }
    
    public virtual void AfterRightEnd()
    {
        
    }

    public override void OnLeftClick(CursorButtonData data) => Machine.Fire(_etLeftClick, data);

    public override void OnLeftRelease(CursorButtonData data) => Machine.Fire(_etLeftRelease, data);

    public override void OnRightClick(CursorButtonData data) => Machine.Fire(_etRightClick, data);

    public override void OnRightRelease(CursorButtonData data) => Machine.Fire(_etRightRelease, data);

    public override void OnMoving(CursorMotionData data)
    {
        Machine.Fire(_etMove, data);
        if(Machine.State == State.HoverInteracting) HoveringInteractor.Interacting(data);
        if(Machine.State == State.LeftInteracting) LeftInteractor.Interacting(data);
        if(Machine.State == State.RightInteracting) RightInteractor.Interacting(data);
    }

    public override void OnKey(InputEventKey key)
    {
        if(AppActions.CancelInteraction.IsJustPressed) Machine.Fire(Event.Cancel);
    }
}

