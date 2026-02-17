using Godot;

namespace Ciallo;

/// <summary>
/// Register a Godot range control to UndoRedo system. 
/// </summary>
public static class RegisterCheckBox
{
    public static CheckBox RegisterUndo(this CheckBox control, CommandManager manager)
    {
        // block inner change to avoid infinite loop.
        bool innerChange = false;
        control.Toggled += toggleOn =>
        {
            if (innerChange)
            {
                innerChange = false;
                return;
            }
            manager.CreateAction("Toggle checkbox " + control.Name);
            // Block error messages on passing CustomCallable with lambda
            Engine.PrintErrorMessages = false;
            manager.AddDoMethod(Callable.From(() =>
            {
                innerChange = true;
                control.SetPressed(toggleOn);
            }));
            manager.AddUndoMethod(Callable.From(delegate
            {
                innerChange = true;
                control.SetPressed(!toggleOn);
            }));
            Engine.PrintErrorMessages = true;
            manager.CommitAction(false);
        };
        return control;
    }
}