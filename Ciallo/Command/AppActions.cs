using Godot;

namespace Ciallo.Command;

/// <summary>
/// Static access to the actions defined in godot editor.
/// We consider actions are those shortcuts defined in the godot editor, while commands are the actual actor.
/// </summary>
/// <remarks>Using GodotSharp.SourceGenerators library</remarks>
[InputMap(nameof(AppAction))]
public static partial class AppActions;

public record AppAction(StringName Name)
{
    public readonly Shortcut Shortcut = new()
    {
        Events =
        [
            new InputEventAction
            {
                Action = Name,
                Pressed = true,
            }
        ],
    };

    public bool IsPressed => Input.IsActionPressed(Name);
    public bool IsJustPressed => Input.IsActionJustPressed(Name);
    public bool IsJustReleased => Input.IsActionJustReleased(Name);
    public float Strength => Input.GetActionStrength(Name);

    public bool IsPressedBy(InputEvent inputEvent) => inputEvent.IsActionPressed(Name);
    public bool IsReleasedBy(InputEvent inputEvent) => inputEvent.IsActionReleased(Name);

    public void Press() => Input.ActionPress(Name);
    public void Release() => Input.ActionRelease(Name);

    public static implicit operator StringName(AppAction input) => input.Name;
}
