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
    public ReactiveProperty<bool> DocumentModified { get; } = new(false);

    public CommandManager()
    {
        SetMaxSteps(30);
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

    public static bool SkipPropertyCommit = false; // not thread safe
    /// <summary>
    /// Make a ReactiveProperty redo undoable
    /// </summary>
    /// <returns> Subscriptions to unregister </returns>
    public CompositeDisposable RegisterProperty<T>(ReactiveProperty<T> property)
    {
        ReactiveProperty<T> old = new(property.Value);
        var subs = new CompositeDisposable();

        // If time within TimeSpan has no more changes, commit the final value.
        property.Skip(1).Where(_ => !SkipPropertyCommit).Debounce(TimeSpan.FromMilliseconds(250)).Subscribe(newValue =>
        {
            if (EqualityComparer<T>.Default.Equals(old.Value, newValue)) return;
            var obj = new PropertyWrapperObject<T>(property, old);
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

    public new void CommitAction(bool execute = true)
    {
        base.CommitAction(execute);
        DocumentModified.Value = true;
    }

    public new void Undo()
    {
        base.Undo();
        DocumentModified.Value = true;
    }

    public new void Redo()
    {
        base.Redo();
        DocumentModified.Value = true;
    }
}

public partial class PropertyWrapperObject<T>(ReactiveProperty<T> property, ReactiveProperty<T> old) : GodotObject
{
    public readonly T NewValue = property.Value;
    public readonly T OldValue = old.Value;

    public void Do()
    {
        CommandManager.SkipPropertyCommit = true;
        property.Value = NewValue;
        CommandManager.SkipPropertyCommit = false;
        old.Value = NewValue;
    }
    public void Undo()
    {
        CommandManager.SkipPropertyCommit = true;
        property.Value = OldValue;
        CommandManager.SkipPropertyCommit = false;
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