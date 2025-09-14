using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using Humanizer;

public abstract partial class ToolButtonBase : Button, IPropertySource
{
    public virtual string ToolName => GetType().Name.Humanize();
    
    /// <remark>
    /// Shen: I guess this is the only design to violate the "who create who delete" rule
    /// </remark>
    public abstract void DrawProperty(PropertyContainer container);
    
    public override void _EnterTree()
    {
        ButtonGroup = ToolManager.ToolButtonGroup;
    }

    public override string ToString() => ToolName;
}