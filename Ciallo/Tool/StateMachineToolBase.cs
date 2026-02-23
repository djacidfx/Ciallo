using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;
using Stateless;

namespace Ciallo.Tool;

using StateMachine = StateMachine<InteractiveSessionBase, StateMachineToolBase.Trigger>;
using StateConfiguration = StateMachine<InteractiveSessionBase, StateMachineToolBase.Trigger>.StateConfiguration;

/// <summary>
/// Create tool with state machine management.
/// </summary>
/// <remarks>
/// See stateless library https://github.com/dotnet-state-machine/stateless for the state machine configuration api.
/// </remarks>
/// <remarks>
/// By product design, all the interactions that involve active user input (not hover) should set key input as handled.
/// i.e. bool OnKey(...) return true;
/// </remarks>
/// <remarks>
/// Initial states are configured to refresh (call End then Start) when user undo/redo.
/// Key design idea: The only source of data change when hovering is undo/redo, so refreshing the session can reduce mind burden.
/// </remarks>
/// <remarks>
/// Prioity: State machine transit > Route to session
/// Prioity of trigger: Godot action > Key > Mouse button.
/// </remarks>
public abstract partial class StateMachineToolBase : ITool
{
    public readonly StateMachine Machine = new(ToolInactive.Instance);

    public Entity Document
    {
        get;
        init
        {
            field = value;
            ConfigureStateMachine();
        }
    }
    public Entity[] WorkingLayers { get; set; }
    public Entity WorkingLayer => WorkingLayers.First();
    public SceneTree GetTree() => (SceneTree)Engine.GetMainLoop();

    private CursorButtonData _lastestCursor;
    private TimeSpan _accumulatedInterval = TimeSpan.Zero;

    private readonly HashSet<AppAction> _triggerActions = [];

    private IDisposable _commandManagerSub;

    protected StateMachineToolBase()
    {
        Machine.Configure(ToolInactive.Instance)
            .Permit(Trigger.Activate, ToolActive.Instance);

        Machine.Configure(ToolActive.Instance)
            .Permit(Trigger.Deactivate, ToolInactive.Instance);
    }

    protected abstract void ConfigureStateMachine();

    public StateConfiguration Configure(InteractiveSessionBase session)
    {
        session.Document = Document;
        return Machine.Configure(session).SubstateOf(ToolActive.Instance)
            .OnEntry(t =>
            {
                t.Source.BeforeDstStart(session);
                t.Destination.Start(_lastestCursor);
                t.Source.AfterDstStart(session);
            })
            .OnExit(t =>
            {
                t.Destination.BeforeSrcEnd(t.Source);
                if (t.Trigger == Trigger.Get(AppActions.CancelInteraction, true) ||
                    t.Trigger == Trigger.Get(AppActions.CancelInteraction, false) ||
                    t.Trigger == Trigger.Deactivate)
                    t.Source.Cancel();
                else t.Source.End(_lastestCursor);
                t.Destination.AfterSrcEnd(t.Source);
            });
    }

    public StateConfiguration ConfigureInitial(InteractiveSessionBase session)
    {
        Machine.Configure(ToolActive.Instance)
            .InitialTransition(session);
        var cfg = Configure(session);
        cfg.PermitReentry(Trigger.Refresh);
        return cfg;
    }

    private IDisposable ObserveCommandManager(Entity document)
    {
        return document.Get<CommandManager>().UndoRedoExecuted.Subscribe(_ =>
        {
            if (Machine.CanFire(Trigger.Refresh))
                Machine.Fire(Trigger.Refresh);
        });
    }

    public virtual void OnActivated() { }
    public virtual void OnDeactivated() { }

    #region ITool

    public void OnMouseButton(InputEventMouseButton button, CursorButtonData data)
    {
        _accumulatedInterval = TimeSpan.Zero;
        _lastestCursor = data;
        var trigger = Trigger.Get(button.ButtonIndex, button.Pressed);
        if (Machine.CanFire(trigger))
            Machine.Fire(trigger);
    }

    public bool OnKey(InputEventKey key)
    {
        // Check trigger action
        var actionTrigger = DetectTriggerAction();
        if (actionTrigger != null && Machine.CanFire(actionTrigger))
        {
            Machine.Fire(actionTrigger);
            return true;
        }
        // Check key
        var keyTrigger = Trigger.Get(key.Keycode, key.Pressed);
        if (Machine.CanFire(keyTrigger))
        {
            Machine.Fire(keyTrigger);
            return true;
        }
        // Route to current session
        return Machine.State.OnKey(key, _lastestCursor);
    }

    private Trigger DetectTriggerAction()
    {
        foreach (var action in _triggerActions)
        {
            if (action.IsJustPressed)
                return Trigger.Get(action, true);
            if (action.IsJustReleased)
                return Trigger.Get(action, false);
        }
        return null;
    }

    public void OnMoving(CursorMotionData data)
    {
        _accumulatedInterval += data.TimeDelta;
        if (_accumulatedInterval > Machine.State.MovingMinInterval)
        {
            CursorMotionData motion = (CursorMotionData)_lastestCursor; // Copy all non-delta
            motion.ScreenDelta = data.ScreenPosition - _lastestCursor.ScreenPosition;
            motion.WorldDelta = data.WorldPosition - _lastestCursor.WorldPosition;
            motion.PressureDelta = data.Pressure - _lastestCursor.Pressure;
            motion.TiltDelta = data.Tilt - _lastestCursor.Tilt;
            motion.TimeDelta = _accumulatedInterval;

            Machine.State.Moving(motion);
            _lastestCursor = data;
            _accumulatedInterval = TimeSpan.Zero;
        }
    }

    public abstract void DrawProperty(PropertyContainer container);
    public abstract bool CanHandleLayer(params Entity[] layerEs);

    public void OnActivate(params Entity[] layerEs)
    {
        foreach (var session in Machine.GetInfo().States.Select(info => (InteractiveSessionBase)info.UnderlyingState))
        {
            session.WorkingLayers = layerEs;
        }
        WorkingLayers = layerEs;
        OnActivated();
        Machine.Fire(Trigger.Activate);
        _commandManagerSub = ObserveCommandManager(Document);
    }

    public void OnDeactivate()
    {
        _commandManagerSub.Dispose();
        Machine.Fire(Trigger.Deactivate);
        OnDeactivated();
        WorkingLayers = null;
        foreach (var session in Machine.GetInfo().States.Select(info => (InteractiveSessionBase)info.UnderlyingState))
        {
            session.WorkingLayers = null;
        }
    }

    #endregion

    #region Triggers

    protected Trigger Press(MouseButton button) => Trigger.Get(button, true);
    protected Trigger Release(MouseButton button) => Trigger.Get(button, false);
    protected Trigger Press(Key key) => Trigger.Get(key, true);
    protected Trigger Release(Key key) => Trigger.Get(key, false);
    protected Trigger Press(AppAction action)
    {
        _triggerActions.Add(action);
        return Trigger.Get(action, true);
    }
    protected Trigger Release(AppAction action)
    {
        _triggerActions.Add(action);
        return Trigger.Get(action, false);
    }

    #endregion
}

#region Internal Tool States

public abstract class InternalToolState : InteractiveSessionBase
{
    public override void Start(CursorButtonData cursor) { }
    public override void End(CursorButtonData cursor) { }
    public override void Cancel() { }
    public override bool OnKey(InputEventKey key, CursorButtonData data) { return false; }
    public override void Moving(CursorMotionData data) { }
}

public class ToolActive : InternalToolState
{
    public static readonly ToolActive Instance = new();
}

public class ToolInactive : InternalToolState
{
    public static readonly ToolInactive Instance = new();
}

#endregion