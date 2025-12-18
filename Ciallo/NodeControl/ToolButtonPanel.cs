using Frent;
using Godot;
using R3;

namespace Ciallo.NodeControl;

/// <remarks>
/// This is a broken abstraction mixing tool button GUI and tool logic data.
/// But for current version, it's acceptable for being lazy.
/// </remarks>
public partial class ToolButtonPanel : Container
{
    // Current design mix tool button GUI and tool logic data, for being lazy
    public ButtonGroup ToolButtonGroup { get; } = new();
    public readonly ReactiveProperty<BaseButton> ActiveToolButton = new(null);

    [OnInstantiate]
    private void Initialise(Entity document)
    {
        // Set button group
        foreach (var child in GetChildren())
        {
            var button = (Button)child;
            button.ButtonGroup = ToolButtonGroup;
        }

        ToolButtonGroup.Pressed += button =>
        {
            ActiveToolButton.Value = button;
        };
    }

    public void DeactivateToolButton()
    {
        ToolButtonGroup.GetPressedButton()?.SetPressedNoSignal(false);
    }
}