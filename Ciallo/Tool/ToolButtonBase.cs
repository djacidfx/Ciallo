using Ciallo.Tool;
using Godot;

public partial class ToolButtonBase : Button
{
    public override void _Ready()
    {
        ButtonGroup = ToolManager.ToolButtonGroup;
    }
}