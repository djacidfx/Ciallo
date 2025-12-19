using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

/// <remarks>
/// This is a broken abstraction mixing tool button GUI and tool logic data.
/// But for current version, it's acceptable for being lazy.
/// </remarks>
[ButtonEnum("ToolButton")]
public partial class ToolButtonPanel : Container
{
    // Current design mix tool button GUI and tool logic data, for being lazy
    public ButtonGroup ToolButtonGroup { get; } = new();
    public readonly ReactiveProperty<ToolButton?> ActiveToolButton = new(0);

    [OnInstantiate]
    private void Initialise(Entity document)
    {
        // Set button group
        foreach (var child in GetChildren())
        {
            var button = (Button)child;
            button.ButtonGroup = ToolButtonGroup;
        }

        ToolButtonGroup.SignalAsObservable<BaseButton>(ButtonGroup.SignalName.Pressed)
            .DistinctUntilChanged()
            .Subscribe(button =>
            {
                var toolButton = GetButtonEnum(button);
                document.Get<ToolManager>().OnSwitchToolButton(toolButton);
            }).AddTo(this);
    }

    public void PressButton(ToolButton toolButton)
    {
        GetButton(toolButton).ButtonPressed = true;
    }

    public void DeactivateToolButton()
    {
        ToolButtonGroup.GetPressedButton()?.SetPressedNoSignal(false);
    }
}