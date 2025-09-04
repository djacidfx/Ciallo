using Godot;

namespace Ciallo.Command;

/// <summary>
/// Static access to the actions defined in godot editor.
/// To distinguish, we consider actions are those shortcuts defined in the godot editor, while commands are the actual actor.
/// </summary>
/// <remarks>Using GodotSharp.SourceGenerators library</remarks>
[InputMap(nameof(AppAction))]
public static partial class AppActions;

public class AppAction(StringName name)
{
    public StringName Name => name;
    public readonly Shortcut Shortcut = new()
    {
        Events = [new InputEventAction
        {
            Action = name,
            Pressed = true,
        }],
    };

    public bool IsPressed => Input.IsActionPressed(name);
    public bool IsJustPressed => Input.IsActionJustPressed(name);
    public bool IsJustReleased => Input.IsActionJustReleased(name);
    public float Strength => Input.GetActionStrength(name);

    public void Press() => Input.ActionPress(name);
    public void Release() => Input.ActionRelease(name);

    public static implicit operator StringName(AppAction input) => input.Name;
}