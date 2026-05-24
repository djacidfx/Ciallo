using Godot;

namespace Ciallo;

/// <summary>
/// Register a ColorPickerButton to command history.
/// </summary>
public static class RegisterColorPicker
{
    public static ColorPickerButton RegisterUndo(this ColorPickerButton control, CommandManager manager)
    {
        if (manager == null)
            return control;
        bool innerChange = false;
        var recordedColor = control.Color;
        control.ColorChanged += newColor =>
        {
            if (innerChange)
            {
                innerChange = false;
                return;
            }

            var oldColor = recordedColor;
            manager.CommitSequence(
                "Change color picker " + control.Name,
                new DelegateCommand(
                    () =>
                    {
                        innerChange = true;
                        control.Color = newColor;
                        control.EmitSignal(ColorPickerButton.SignalName.ColorChanged, newColor);
                        recordedColor = newColor;
                    },
                    () =>
                    {
                        innerChange = true;
                        control.Color = oldColor;
                        control.EmitSignal(ColorPickerButton.SignalName.ColorChanged, oldColor);
                        recordedColor = oldColor;
                    }),
                execute: false);
            recordedColor = newColor;
        };
        return control;
    }
}
