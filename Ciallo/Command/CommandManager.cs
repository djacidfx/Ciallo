using System;
using Godot;

namespace Ciallo.Command;

public class CommandManager : IDisposable
{
    // Shen: Has been tortured several hours by Godot's object management system.
    private readonly UndoRedo _undoRedo = new();

    public bool Undo() => _undoRedo.Undo();
    public bool Redo() => _undoRedo.Redo();
    public void ClearHistory() => _undoRedo.ClearHistory();
    public int HistoryCount =>_undoRedo.GetHistoryCount();
    
    public void Commit(CommandBase command, bool execute = true)
    {
        _undoRedo.CreateAction(command.Name);
        _undoRedo.AddDoMethod(new(command, CommandBase.MethodName.Do));
        _undoRedo.AddDoReference(command);
        _undoRedo.AddUndoMethod(new(command, CommandBase.MethodName.Undo));
        _undoRedo.AddUndoReference(command);
        _undoRedo.CommitAction(execute);
    }

    private void ReleaseUnmanagedResources()
    {
        _undoRedo.Free();
    }
    
    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            _undoRedo?.Dispose();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    ~CommandManager()
    {
        Dispose(false);
    }
}