using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

/// <remarks>
/// Button enum generate an enum containing all buttons' node name in the tool button panel.
/// </remarks>
[ButtonEnum("ToolButton")]
public partial class ToolButtonPanel : Container
{
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