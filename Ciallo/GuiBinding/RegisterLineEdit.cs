using Godot;

namespace Ciallo;

/// <summary>
/// Register a LineEdit to UndoRedo system. 
/// </summary>
public static class RegisterLineEdit
{
    public static LineEdit RegisterUndo(this LineEdit control, CommandManager manager)
    {
        if (manager == null)
            return control;
        bool innerChange = false;
        var recordedText = control.Text;
        control.TextChanged += newText =>
        {
            if (innerChange)
            {
                innerChange = false;
                return;
            }

            var oldText = recordedText;
            manager.CreateAction("Change line edit " + control.GetInstanceId(), UndoRedo.MergeMode.Ends);
            // Block error messages on passing CustomCallable with lambda
            Engine.PrintErrorMessages = false;
            manager.AddDoMethod(Callable.From(() =>
            {
                innerChange = true;
                control.Text = newText;
                control.EmitSignal(LineEdit.SignalName.TextSubmitted, newText);
                recordedText = newText;
            }));
            manager.AddUndoMethod(Callable.From(() =>
            {
                innerChange = true;
                control.Text = oldText;
                control.EmitSignal(LineEdit.SignalName.TextSubmitted, oldText);
                recordedText = oldText;
            }));
            Engine.PrintErrorMessages = true;
            manager.CommitAction(false);
            recordedText = newText;
        };
        return control;
    }
}