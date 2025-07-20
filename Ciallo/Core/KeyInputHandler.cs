using Godot;
using System;

namespace Ciallo.Core;

public partial class KeyInputHandler : Node
{
    /// <summary>
    /// Using `UnhandledKeyInput` can get keyboard events that is not handled by GUI controls, e.g. `LineEdit`.
    /// Pitfall: Bug 4.4.1. The `TextEdit` control does not correctly mark key release events as handled.
    /// Thus, we need to deal with released events in the UnhandledKeyInput`.
    /// </summary>
    public override void _UnhandledKeyInput(InputEvent e)
    {
        // Pitfall: "IsActionPressed" return true for both Redo and Undo actions when pressing Ctrl+Shift+Z.
        
    }
}
