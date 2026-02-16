using Ciallo.Command;
using Godot;
using R3;

// ReSharper disable once CheckNamespace
namespace Ciallo;

/// <summary>
/// Inherits from UndoRedo with extra methods to manage commands.
/// </summary>
public partial class CommandManager : UndoRedo
{
    public readonly ReactiveProperty<bool> DocumentModified = new(false);
    public readonly Subject<bool> UndoRedoExecuted = new(); // true is undo, false is redo

    public CommandManager()
    {
        SetMaxSteps(3);
    }

    public void AddDo(CommandWrapperObject cmdWrapper)
    {
        AddDoMethod(new(cmdWrapper, CommandWrapperObject.MethodName.Do));
        AddDoReference(cmdWrapper.DoDeleter);
        AddDoReference(cmdWrapper); // order matters, first add first delete
    }

    public void AddUndo(CommandWrapperObject cmdWrapper)
    {
        AddUndoMethod(new(cmdWrapper, CommandWrapperObject.MethodName.Undo));
        AddUndoReference(cmdWrapper.UndoDeleter);
        AddUndoReference(cmdWrapper); // order matters
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
        UndoRedoExecuted.OnNext(true);
    }

    public new void Redo()
    {
        base.Redo();
        DocumentModified.Value = true;
        UndoRedoExecuted.OnNext(false);
    }
}

public partial class CommandWrapperObject(ICommand command) : GodotObject
{
    public ICommand Command { get; } = command;
    public ObjectDeleter DoDeleter = new(command, true);
    public ObjectDeleter UndoDeleter = new(command, false);

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            // If instance is not freed by UndoRedo, free here.
            if (IsInstanceValid(DoDeleter))
                DoDeleter.FreeWithoutDeleting();
            if (IsInstanceValid(UndoDeleter))
                UndoDeleter.FreeWithoutDeleting();
        }
    }

    public void Do() => Command.Do();
    public void Undo() => Command.Undo();
}

/// <summary>
/// Automatically destroy entities and objects when the command is deleted, unless FreeWithoutDestroying is called.
/// </summary>
public partial class ObjectDeleter(ICommand cmd, bool isDo) : GodotObject
{
    private bool _delete = true;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_delete) return;

        var entities = isDo ? cmd.DoRefEntities : cmd.UndoRefEntities;
        if (entities != null)
        {
            foreach (var e in entities)
                e.Delete();
        }

        var objects = isDo ? cmd.DoRefObjects : cmd.UndoRefObjects;
        if (objects != null)
        {
            foreach (var obj in objects)
                obj?.Free();
        }
    }

    public void FreeWithoutDeleting()
    {
        _delete = false;
        Free();
    }
}