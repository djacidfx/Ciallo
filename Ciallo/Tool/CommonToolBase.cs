using Ciallo.Command;
using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
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
public abstract class CommonToolBase : ITool
{
    public InteractorBase LeftInteractor
    {
        get => _leftInteractor;
        set
        {
            bool isActive = _machine.IsInState(State.ToolActive);
            if (isActive) _machine.Fire(Event.Deactivate);
            _leftInteractor = value;
            if (isActive) _machine.Fire(Event.Activate);
        }
    }

    public HoverBase HoverInteractor
    {
        get => _hoverInteractor;
        set
        {
            bool isActive = _machine.IsInState(State.ToolActive);
            if (isActive) _machine.Fire(Event.Deactivate);
            _hoverInteractor = value;
            if (isActive) _machine.Fire(Event.Activate);
        }
    }

    public InteractorBase RightInteractor
    {
        get => _rightInteractor;
        set
        {
            bool isActive = _machine.IsInState(State.ToolActive);
            if (isActive) _machine.Fire(Event.Deactivate);
            _rightInteractor = value;
            if (isActive) _machine.Fire(Event.Activate);
        }
    }

    public enum State
    {
        ToolInactive,
        ToolActive,

        Idle,
        Hovering,
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
        Cancel,
        RefreshHover,
        Activate,
        Deactivate,
    }

    public Entity Document { get; init; }
    public Entity WorkingLayerE;
    public SceneTree GetTree() => (SceneTree)Engine.GetMainLoop();
    private readonly StateMachine<State, Event> _machine = new(State.ToolInactive);
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

        bool InteractorStartGuard(InteractorBase interactor, CursorButtonData data)
        {
            return interactor?.Prepare(data) == true;
        }

        _machine.Configure(State.ToolInactive)
            .Permit(Event.Activate, State.ToolActive);
        _machine.Configure(State.ToolActive)
            .Permit(Event.Deactivate, State.ToolInactive)
            .InitialTransition(State.Idle);

        _machine.Configure(State.Idle).SubstateOf(State.ToolActive)
            .OnEntry(() =>
            {
                if (HoverInteractor != null) _machine.Fire(Event.RefreshHover);
            })
            .PermitIf(_etLeftClick, State.LeftInteracting, data => InteractorStartGuard(LeftInteractor, data))
            .PermitIf(_etRightClick, State.RightInteracting, data => InteractorStartGuard(RightInteractor, data))
            .PermitIf(_etMove, State.Hovering)
            .PermitIf(Event.RefreshHover, State.Hovering);

        _machine.Configure(State.Hovering).SubstateOf(State.ToolActive)
            .OnEntry(() => HoverInteractor.Start())
            .PermitIf(_etLeftClick, State.LeftInteracting, data => InteractorStartGuard(LeftInteractor, data))
            .PermitIf(_etRightClick, State.RightInteracting, data => InteractorStartGuard(RightInteractor, data))
            .PermitReentry(Event.RefreshHover)
            .Permit(Event.Cancel, State.Idle)
            .OnExit(() => HoverInteractor.End());

        _machine.Configure(State.LeftInteracting).SubstateOf(State.ToolActive)
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

        _machine.Configure(State.RightInteracting).SubstateOf(State.ToolActive)
            .OnEntryFrom(_etRightClick, data =>
            {
                BeforeRightStart();
                RightInteractor.Start(data);
            })
            .Permit(Event.Cancel, State.Idle)
            .PermitIf(_etRightRelease, State.Idle)
            .PermitIf(_etLeftClick, State.Idle)
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

    public bool OnLeftClick(CursorButtonData data)
    {
        _machine.Fire(_etLeftClick, data);
        return true;
    }

    public bool OnLeftRelease(CursorButtonData data)
    {
        _machine.Fire(_etLeftRelease, data);
        return false;
    }

    public bool OnRightClick(CursorButtonData data)
    {
        _machine.Fire(_etRightClick, data);
        return true;
    }

    public bool OnRightRelease(CursorButtonData data)
    {
        _machine.Fire(_etRightRelease, data);
        return false;
    }

    public void OnMoving(CursorMotionData data)
    {
        _machine.Fire(_etMove, data);
        if (_machine.State == State.Hovering)
            HoverInteractor.Interacting(data);
        if (_machine.State == State.LeftInteracting)
            LeftInteractor.Interacting(data);
        if (_machine.State == State.RightInteracting)
            RightInteractor.Interacting(data);
    }

    public virtual ToolKeyActions OnKey(InputEventKey key)
    {
        if (AppActions.CancelInteraction.IsJustPressed)
        {
            _machine.Fire(Event.Cancel);
            return ToolKeyActions.HandleInput;
        }
        return ToolKeyActions.None;
    }

    public abstract void DrawProperty(PropertyContainer container);
    public abstract bool CanHandleLayer(params Entity[] layerEs);

    public virtual void OnActivate(params Entity[] layerEs)
    {
        WorkingLayerE = layerEs.Length > 0 ? layerEs[0] : Entity.Null;
        _machine.Fire(Event.Activate);
    }

    public virtual void OnDeactivate()
    {
        _machine.Fire(Event.Deactivate);
        WorkingLayerE = Entity.Null;
    }

    public void FireRefreshHover() => _machine.Fire(Event.RefreshHover);
}