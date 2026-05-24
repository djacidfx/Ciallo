using Godot;

namespace Ciallo.GuiControl;

public partial class LayerPanelContainer : MarginContainer
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
    }
}