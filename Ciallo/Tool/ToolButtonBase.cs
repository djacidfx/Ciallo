using Ciallo.Tool;
using Godot;
using Humanizer;

public partial class ToolButtonBase : Button
{
    public virtual string Name => GetType().Name.Humanize();
    
    public override void _Ready()
    {
        ButtonGroup = ToolManager.ToolButtonGroup;
    }

    public override string ToString() => Name;
}