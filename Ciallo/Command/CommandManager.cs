using System;
using System.Collections.Generic;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

/// <summary>
/// Inherits from UndoRedo with extra methods to manage commands.
/// </summary>
public partial class CommandManager : UndoRedo
{
    public CommandManager()
    {
        SetMaxSteps(3); // fast invoke bugs
    }

    public void AddDo(CommandWrapperObject cmdWrapper)
    {
        cmdWrapper.DoWrapper = new ObjectWrapper(cmdWrapper.Command.DoRefEntities, cmdWrapper.Command.DoRefObjects);
        AddDoMethod(new(cmdWrapper, CommandWrapperObject.MethodName.Do));
        AddDoReference(cmdWrapper.DoWrapper);
        AddDoReference(cmdWrapper);
    }

    public void AddUndo(CommandWrapperObject cmdWrapper)
    {
        cmdWrapper.UndoWrapper = new ObjectWrapper(cmdWrapper.Command.UndoRefEntities, cmdWrapper.Command.UndoRefObjects);
        AddUndoMethod(new(cmdWrapper, CommandWrapperObject.MethodName.Undo));
        AddUndoReference(cmdWrapper.UndoWrapper);
        AddUndoReference(cmdWrapper);
    }

    /// <summary>
    /// Make a ReactiveProperty redo undoable
    /// </summary>
    /// <returns> Subscriptions to unregister </returns>
    public CompositeDisposable RegisterProperty<T>(ReactiveProperty<T> property) where T : struct
    {
        ReactiveProperty<T> old = new(property.Value);
        ReactiveProperty<bool> skipCommit = new(false);
        var subs = new CompositeDisposable();

        property.Skip(1).Where(_ => !skipCommit.Value).Debounce(TimeSpan.FromMilliseconds(350)).Subscribe(newValue =>
        {
            var obj = new PropertyWrapperObject<T>(property, skipCommit, old);
            CreateAction("Property Change");
            AddDoMethod(new(obj, PropertyWrapperObject<T>.MethodName.Do));
            AddDoReference(obj);
            AddUndoReference(obj);
            AddUndoMethod(new(obj, PropertyWrapperObject<T>.MethodName.Undo));
            CommitAction(false); // Property already been the value
            old.Value = newValue;
        }).AddTo(subs);

        return subs;
    }
}

public partial class PropertyWrapperObject<T>(ReactiveProperty<T> property, ReactiveProperty<bool> skip, ReactiveProperty<T> old) : GodotObject where T : struct
{
    public T NewValue = property.Value;
    public T OldValue = old.Value;

    public void Do()
    {
        skip.Value = true;
        property.Value = NewValue;
        skip.Value = false;
        old.Value = NewValue;
    }
    public void Undo()
    {
        skip.Value = true;
        property.Value = OldValue;
        skip.Value = false;
        old.Value = OldValue;
    }
}

public partial class CommandWrapperObject(CommandBase command) : GodotObject
{
    public CommandBase Command { get; } = command;
    public ObjectWrapper DoWrapper;
    public ObjectWrapper UndoWrapper;

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (IsInstanceValid(DoWrapper))
                DoWrapper.FreeWithoutDestroying();
            if (IsInstanceValid(UndoWrapper))
                UndoWrapper.FreeWithoutDestroying();
        }
    }

    public void Do() => Command.Do();
    public void Undo() => Command.Undo();
}

/// <summary>
/// Wrapper for the IEnumerable to be used in CommandBase.
/// Automatically destroy entities and objects when the command is deleted, unless FreeWithoutDestroying is called.
/// </summary>
public partial class ObjectWrapper(IEnumerable<Entity> entities, IEnumerable<GodotObject> objects) : GodotObject
{
    private bool _destroy = true;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_destroy) return;

        if (entities != null)
        {
            foreach (var e in entities)
                e.Delete();
        }

        if (objects != null)
        {
            foreach (var obj in objects)
                obj.Free();
        }
    }

    public void FreeWithoutDestroying()
    {
        _destroy = false;
        Free();
    }
}