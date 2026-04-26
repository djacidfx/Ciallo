using Ciallo.Command;
using Frent.Components;
using Godot;
using R3;

// ReSharper disable once CheckNamespace
namespace Ciallo;

/// <summary>
/// Inherits from UndoRedo with extra methods to manage commands.
/// </summary>
public partial class CommandManager : UndoRedo, IDestroyable
{
    private ulong _savedVersion;
    private readonly ReactiveProperty<bool> _documentModified = new(false);
    public ReadOnlyReactiveProperty<bool> DocumentModified => _documentModified;
    public readonly Subject<bool> UndoRedoExecuted = new(); // true is undo, false is redo

    public CommandManager()
    {
        SetMaxSteps(3);
        _savedVersion = GetVersion();
        VersionChanged += () =>
        {
            // Note UndoRedo invoke VersionChanged at free, so check if alive
            if (IsInstanceValid(this))
                _documentModified.Value = _savedVersion != GetVersion();
        };
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
        _documentModified.Value = true;
    }

    public new void Undo()
    {
        if (!HasUndo()) return;
        base.Undo();
        UndoRedoExecuted.OnNext(true);
    }

    public new void Redo()
    {
        if (!HasRedo()) return;
        base.Redo();
        UndoRedoExecuted.OnNext(false);
    }

    public void OnSave()
    {
        _savedVersion = GetVersion();
        _documentModified.Value = false;
    }

    public void Destroy()
    {
        Free();
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
/// Delete unless FreeWithoutDestroying is called.
/// </summary>
public partial class ObjectDeleter(ICommand cmd, bool isDo) : GodotObject
{
    private bool _toDelete = true;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_toDelete) return;

        if (isDo)
            cmd.OnDeletedAsDo();
        else
            cmd.OnDeletedAsUndo();
    }

    public void FreeWithoutDeleting()
    {
        _toDelete = false;
        Free();
    }
}