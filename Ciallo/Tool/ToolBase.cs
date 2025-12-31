using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;
using Stateless;

namespace Ciallo.Tool;

using StateMachine = StateMachine<IInteractiveSession, ToolBase.Trigger>;
using StateConfiguration = StateMachine<IInteractiveSession, ToolBase.Trigger>.StateConfiguration;

public abstract partial class ToolBase : ITool
{
    private CursorButtonData _currentCursor;
    private readonly HashSet<AppAction> _triggerActions = new();

    public virtual void OnActivated(params Entity[] layerEs) { }
    public virtual void OnDeactivated() { }

    public readonly StateMachine Machine = new(ToolInactive.Instance);

    protected ToolBase()
    {
        Machine.Configure(ToolInactive.Instance)
            .Permit(Trigger.Activate, ToolActive.Instance);

        Machine.Configure(ToolActive.Instance)
            .Permit(Trigger.Deactivate, ToolInactive.Instance);
    }

    public StateConfiguration Configure(IInteractiveSession session)
    {
        return Machine.Configure(session).SubstateOf(ToolActive.Instance)
            .OnEntry(t =>
            {
                t.Source.BeforeDstStart(session);
                t.Destination.Start(_currentCursor);
                t.Source.AfterDstStart(session);
            })
            .OnExit(t =>
            {
                t.Destination.BeforeSrcEnd(t.Source);
                if (t.Trigger == Trigger.Get(AppActions.CancelInteraction, true) ||
                    t.Trigger == Trigger.Get(AppActions.CancelInteraction, false) ||
                    t.Trigger == Trigger.Deactivate)
                    t.Source.Cancel();
                else t.Source.End(_currentCursor);
                t.Destination.AfterSrcEnd(t.Source);
            });
    }

    public StateConfiguration ConfigureInitial(IInteractiveSession session)
    {
        Machine.Configure(ToolActive.Instance)
            .InitialTransition(session);
        var cfg = Configure(session);
        cfg.PermitReentry(Trigger.Refresh);
        return cfg;
    }

    #region ITool

    public void OnMouseButton(InputEventMouseButton button, CursorButtonData data)
    {
        _currentCursor = data;
        var trigger = Trigger.Get(button.ButtonIndex, button.Pressed);

        if (Machine.CanFire(trigger))
            Machine.Fire(trigger);
    }

    public bool OnKey(InputEventKey key)
    {
        // Check trigger action
        var actionTrigger = DetectTriggerAction();
        if (actionTrigger != null)
        {
            if (Machine.CanFire(actionTrigger))
            {
                Machine.Fire(actionTrigger);
                return true;
            }
        }
        // Check key
        var keyTrigger = Trigger.Get(key.Keycode, key.Pressed);
        if (Machine.CanFire(keyTrigger))
        {
            Machine.Fire(keyTrigger);
            return true;
        }
        // Route to current session
        return Machine.State.OnKey(key, _currentCursor);
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
        _currentCursor = data;
        Machine.State.Interacting(data);
    }

    public abstract void DrawProperty(PropertyContainer container);
    public abstract bool CanHandleLayer(params Entity[] layerEs);

    public void OnActivate(params Entity[] layerEs)
    {
        OnActivated(layerEs);
        Machine.Fire(Trigger.Activate);
    }

    public void OnDeactivate()
    {
        Machine.Fire(Trigger.Deactivate);
        OnDeactivated();
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

public abstract class InternalToolState : IInteractiveSession
{
    public void Start(CursorButtonData cursor) { }
    public void End(CursorButtonData cursor) { }
    public void Cancel() { }
    public bool OnKey(InputEventKey key, CursorButtonData data) { return false; }
    public void Interacting(CursorMotionData data) { }
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