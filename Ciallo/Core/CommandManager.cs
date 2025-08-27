using Godot;

namespace Ciallo.Core;

public class CommandManager
{
    private static UndoRedo _undoRedo = new();
    
    ~CommandManager()
    {
        _undoRedo.Dispose();
    }
}