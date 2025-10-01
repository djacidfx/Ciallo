using Ciallo.Command;
using Ciallo.NodeControl;
using Godot;
using Stateless;

namespace Ciallo.Tool;

using TriggerButton = StateMachine<CommonToolBase.State,CommonToolBase.Event>.TriggerWithParameters<CursorButtonData>;
using TriggerMotion = StateMachine<CommonToolBase.State,CommonToolBase.Event>.TriggerWithParameters<CursorMotionData>;

/// <summary>
/// The base class for common tools that need for three states: Hovering, left mouse drag, right mouse drag.
/// (Middle mouse has dedicated usage for canvas navigation.)
/// For more complex tools, we can write state machine code and implement ITool directly.
/// </summary>
public abstract partial class CommonToolBase : ToolButtonBase, ITool
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

    public readonly StateMachine<State, Event> Machine = new(State.Idle);
    private readonly TriggerButton _tLeftClick;
    private readonly TriggerButton _tLeftRelease;
    private readonly TriggerButton _tRightClick;
    private readonly TriggerButton _tRightRelease;
    private readonly TriggerMotion _tMove;

    protected CommonToolBase()
    {
        Machine.OnUnhandledTrigger((_, _) => { }); // Do nothing on unhandled trigger
        _tLeftClick = Machine.SetTriggerParameters<CursorButtonData>(Event.LeftClick);
        _tLeftRelease = Machine.SetTriggerParameters<CursorButtonData>(Event.LeftRelease);
        _tRightClick = Machine.SetTriggerParameters<CursorButtonData>(Event.RightClick);
        _tRightRelease = Machine.SetTriggerParameters<CursorButtonData>(Event.RightRelease);
        _tMove = Machine.SetTriggerParameters<CursorMotionData>(Event.Move);

        Machine.Configure(State.Idle)
            .PermitIf(_tLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_tRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .PermitIf(_tMove, State.HoverInteracting, _ => HoveringInteractor?.CanInteract == true);
        Machine.Configure(State.HoverInteracting)
            .PermitIf(_tLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_tRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .Permit(Event.Cancel, State.Idle)
            .OnExit(() => HoveringInteractor.Cancel());
        Machine.Configure(State.LeftInteracting)
            .OnEntryFrom(_tLeftClick, data =>
            {
                BeforeLeftStart();
                LeftInteractor.Start(data);
            })
            .Permit(Event.Cancel, State.Idle)
            .PermitIf(_tLeftRelease, State.Idle)
            .OnExit(t =>
            {
                if(t.Trigger == Event.LeftRelease) LeftInteractor.End((CursorButtonData)t.Parameters[0]);
                if(t.Trigger == Event.Cancel) LeftInteractor.Cancel();
                AfterLeftEnd();
            });
        Machine.Configure(State.RightInteracting)
            .OnEntryFrom(_tRightClick, data =>
            {
                BeforeRightStart();
                RightInteractor.Start(data);
            })
            .Permit(Event.Cancel, State.Idle)
            .PermitIf(_tRightRelease, State.Idle)
            .OnExit(t =>
            {
                if(t.Trigger == Event.RightRelease) RightInteractor.End((CursorButtonData)t.Parameters[0]);
                if(t.Trigger == Event.Cancel) RightInteractor.Cancel();
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

    public void OnLeftClick(CursorButtonData data) => Machine.Fire(_tLeftClick, data);

    public void OnLeftRelease(CursorButtonData data) => Machine.Fire(_tLeftRelease, data);

    public void OnRightClick(CursorButtonData data) => Machine.Fire(_tRightClick, data);

    public void OnRightRelease(CursorButtonData data) => Machine.Fire(_tRightRelease, data);

    public void OnMoving(CursorMotionData data)
    {
        Machine.Fire(_tMove, data);
        if(Machine.State == State.HoverInteracting) HoveringInteractor.Interacting(data);
        if(Machine.State == State.LeftInteracting) LeftInteractor.Interacting(data);
        if(Machine.State == State.RightInteracting) RightInteractor.Interacting(data);
    }

    public void OnKey(InputEventKey key)
    {
        if(AppActions.CancelInteraction.IsJustPressed) Machine.Fire(Event.Cancel);
    }
}

