using Ciallo.Command;
using Ciallo.NodeControl;
using Godot;
using Stateless;

namespace Ciallo.Tool;

using buttonParameterEvent = StateMachine<CommonToolBase.State, CommonToolBase.Event>.TriggerWithParameters<CursorButtonData>;
using motionParameterEvent = StateMachine<CommonToolBase.State, CommonToolBase.Event>.TriggerWithParameters<CursorMotionData>;

/// <summary>
/// The base class for common tools that need for three states: Hovering, left mouse drag, right mouse drag.
/// (Middle mouse has dedicated usage for canvas navigation.)
/// For more complex tools, we can write state machine code and implement ITool directly.
/// </summary>
public abstract partial class CommonToolBase : ToolButtonBase
{
    public InteractorBase LeftInteractor
    {
        get => _leftInteractor;
        set
        {
            FireCancel();
            _leftInteractor = value;
        }
    }

    public HoverBase HoverInteractor
    {
        get => _hoverInteractor;
        set
        {
            FireCancel();
            _hoverInteractor = value;
        }
    }

    public InteractorBase RightInteractor
    {
        get => _rightInteractor;
        set
        {
            FireCancel();
            _rightInteractor = value;
        }
    }

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

    private readonly StateMachine<State, Event> _machine = new(State.Idle);
    private readonly buttonParameterEvent _etLeftClick;
    private readonly buttonParameterEvent _etLeftRelease;
    private readonly buttonParameterEvent _etRightClick;
    private readonly buttonParameterEvent _etRightRelease;
    private readonly motionParameterEvent _etMove;
    
    private InteractorBase _rightInteractor;
    private HoverBase _hoverInteractor;
    private InteractorBase _leftInteractor;

    protected CommonToolBase()
    {
        _machine.OnUnhandledTrigger((_, _) => { }); // Do nothing on unhandled trigger
        _etLeftClick = _machine.SetTriggerParameters<CursorButtonData>(Event.LeftClick);
        _etLeftRelease = _machine.SetTriggerParameters<CursorButtonData>(Event.LeftRelease);
        _etRightClick = _machine.SetTriggerParameters<CursorButtonData>(Event.RightClick);
        _etRightRelease = _machine.SetTriggerParameters<CursorButtonData>(Event.RightRelease);
        _etMove = _machine.SetTriggerParameters<CursorMotionData>(Event.Move);

        _machine.Configure(State.Idle)
            .PermitIf(_etLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_etRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .PermitIf(_etMove, State.HoverInteracting, _ => HoverInteractor?.CanInteract == true);
        _machine.Configure(State.HoverInteracting)
            .OnEntryFrom(_etMove, data => HoverInteractor.Start(data))
            .PermitIf(_etLeftClick, State.LeftInteracting, _ => LeftInteractor?.CanInteract == true)
            .PermitIf(_etRightClick, State.RightInteracting, _ => RightInteractor?.CanInteract == true)
            .Permit(Event.Cancel, State.Idle)
            .OnExit(() => HoverInteractor.Cancel());
        _machine.Configure(State.LeftInteracting)
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
                if (t.Trigger == Event.Cancel) LeftInteractor.Cancel();
                else LeftInteractor.End((CursorButtonData)t.Parameters[0]);
                AfterLeftEnd();
            });
        _machine.Configure(State.RightInteracting)
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
                if (t.Trigger == Event.Cancel) RightInteractor.Cancel();
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

    public override void OnLeftClick(CursorButtonData data) => _machine.Fire(_etLeftClick, data);

    public override void OnLeftRelease(CursorButtonData data) => _machine.Fire(_etLeftRelease, data);

    public override void OnRightClick(CursorButtonData data) => _machine.Fire(_etRightClick, data);

    public override void OnRightRelease(CursorButtonData data) => _machine.Fire(_etRightRelease, data);

    public override void OnMoving(CursorMotionData data)
    {
        _machine.Fire(_etMove, data);
        if (_machine.State == State.HoverInteracting) HoverInteractor.Interacting(data);
        if (_machine.State == State.LeftInteracting) LeftInteractor.Interacting(data);
        if (_machine.State == State.RightInteracting) RightInteractor.Interacting(data);
    }

    public override void OnKey(InputEventKey key)
    {
        if (AppActions.CancelInteraction.IsJustPressed) FireCancel();
    }

    public override void OnDeactivate() => FireCancel();

    public void FireCancel() => _machine.Fire(Event.Cancel);
}