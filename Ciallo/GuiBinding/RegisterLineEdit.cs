using Godot;

namespace Ciallo;

/// <summary>
/// Register a LineEdit to command history.
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
            manager.CommitSequence(
                "Change line edit " + control.Name,
                new DelegateCommand(
                    () =>
                    {
                        innerChange = true;
                        control.Text = newText;
                        control.EmitSignal(LineEdit.SignalName.TextSubmitted, newText);
                        recordedText = newText;
                    },
                    () =>
                    {
                        innerChange = true;
                        control.Text = oldText;
                        control.EmitSignal(LineEdit.SignalName.TextSubmitted, oldText);
                        recordedText = oldText;
                    }),
                execute: false);
            recordedText = newText;
        };
        return control;
    }
}
