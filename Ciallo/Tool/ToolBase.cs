using System;
using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;
using Stateless;

namespace Ciallo.Tool;

using StateMachine = StateMachine<IInteractiveSession, ToolBase.Event>;
using StateConfiguration = StateMachine<IInteractiveSession, ToolBase.Event>.StateConfiguration;

public abstract class ToolBase : ITool
{
    private CursorButtonData _currentCursor;

    public enum Event
    {
        Activate,
        Deactivate,
        Refresh,
        Cancel,
    }

    public virtual void OnActivated(params Entity[] layerEs) { }
    public virtual void OnDeactivated() { }

    private readonly StateMachine _machine = new(ToolInactive.Instance);

    protected ToolBase()
    {
        _machine.Configure(ToolInactive.Instance)
            .Permit(Event.Activate, ToolActive.Instance);

        _machine.Configure(ToolActive.Instance)
            .Permit(Event.Deactivate, ToolInactive.Instance);
    }

    public StateConfiguration Configure(IInteractiveSession session)
    {
        return _machine.Configure(session).SubstateOf(ToolActive.Instance)
            .OnEntry(t =>
            {
                t.Source.BeforeDstStart(session);
                t.Destination.Start(_currentCursor);
                t.Source.AfterDstStart(session);
            })
            .OnExit(t =>
            {
                t.Destination.BeforeSrcEnd(t.Source);
                if (t.Trigger is Event.Cancel or Event.Deactivate)
                    t.Source.Cancel();
                else t.Source.End(_currentCursor);
                t.Destination.AfterSrcEnd(t.Source);
            });
    }

    public StateConfiguration ConfigureInitial(IInteractiveSession session)
    {
        _machine.Configure(ToolActive.Instance)
            .InitialTransition(session);
        var cfg = Configure(session);
        cfg.PermitReentry(Event.Refresh);
        return cfg;
    }

    #region ITool

    public void OnMouseButton(InputEventMouseButton button, CursorButtonData data)
    {
        _currentCursor = data;
        throw new NotImplementedException();
    }

    public bool OnKey(InputEventKey key)
    {
        if (IsTriggerEvent(key))
        {
            throw new NotImplementedException();
            return true;
        }
        return _machine.State.OnKey(key, _currentCursor);
    }

    private bool IsTriggerEvent(InputEventKey key)
    {
        throw new NotImplementedException();
    }

    public void OnMoving(CursorMotionData data)
    {
        _currentCursor = data;
        _machine.State.Interacting(data);
    }

    public abstract void DrawProperty(PropertyContainer container);
    public abstract bool CanHandleLayer(params Entity[] layerEs);

    public void OnActivate(params Entity[] layerEs)
    {
        OnActivated(layerEs);
        _machine.Fire(Event.Activate);
    }

    public void OnDeactivate()
    {
        _machine.Fire(Event.Deactivate);
        OnDeactivated();
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