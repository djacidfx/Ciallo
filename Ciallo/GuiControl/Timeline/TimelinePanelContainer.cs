using Godot;

namespace Ciallo.GuiControl;

public partial class TimelinePanelContainer : MarginContainer
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
    }
}