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

    [OnInstantiate]
    private void Initialise()
    {
        // Set button group
        foreach (var child in GetChildren())
        {
            var button = (Button)child;
            button.ButtonGroup = ToolButtonGroup;
        }
    }

    public ToolButtonPanel Bind(ReactiveProperty<ToolButton?> property)
    {
        property.Subscribe(toolButton =>
        {
            if (toolButton == null)
                UnpressActiveButton();
            else
                PressButton(toolButton.Value);
        }).AddTo(this);

        ToolButtonGroup.SignalAsObservable<BaseButton>(ButtonGroup.SignalName.Pressed)
            .DistinctUntilChanged()
            .Subscribe(button =>
            {
                var toolButton = GetButtonEnum(button);
                property.Value = toolButton;
            }).AddTo(this);

        return this;
    }

    public void PressButton(ToolButton toolButton)
    {
        GetButton(toolButton).ButtonPressed = true;
    }

    public void UnpressActiveButton()
    {
        ToolButtonGroup.GetPressedButton()?.SetPressedNoSignal(false);
    }
}