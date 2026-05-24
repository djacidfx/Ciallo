using Godot;

namespace Ciallo.GuiControl;

public partial class PaintPanelContainer : Control
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
    }
}