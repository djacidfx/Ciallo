using Godot;

namespace Ciallo;

/// <summary>
/// Register a ColorPickerButton to UndoRedo system. 
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
            manager.CreateAction("Change color picker " + control.GetInstanceId(), UndoRedo.MergeMode.Ends);
            // Block error messages on passing CustomCallable with lambda
            Engine.PrintErrorMessages = false;
            manager.AddDoMethod(Callable.From(() =>
            {
                innerChange = true;
                control.Color = newColor;
                control.EmitSignal(ColorPickerButton.SignalName.ColorChanged, newColor);
                recordedColor = newColor;
            }));
            manager.AddUndoMethod(Callable.From(() =>
            {
                innerChange = true;
                control.Color = oldColor;
                control.EmitSignal(ColorPickerButton.SignalName.ColorChanged, oldColor);
                recordedColor = newColor;
            }));
            Engine.PrintErrorMessages = true;
            manager.CommitAction(false);
            recordedColor = newColor;
        };
        return control;
    }
}