using Godot;
using System;

namespace Ciallo.Core;

public partial class KeyInputHandler : Node
{
    /// <summary>
    /// Using `UnhandledKeyInput` can get keyboard events that is not handled by GUI controls, e.g. `LineEdit`.
    /// Pitfall: Bug 4.4.1. The `TextEdit` control does not correctly handle key release events, key released events can be received here.
    /// </summary>
    /// <param name="e"></param>
    public override void _UnhandledKeyInput(InputEvent e)
    {
        // Pitfall: Redo and Undo events are triggered at the same time when pressing Ctrl+Shift+Z.
        
    }
}