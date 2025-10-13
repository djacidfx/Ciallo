using System.Collections.Generic;
using System.Linq;
using Ciallo.Tool;
using Godot;
using R3;

public partial class ToolButtonPanel : Container
{
    // Current design mix tool button GUI and tool logic data, for being lazy
    public ButtonGroup ToolButtonGroup { get; } = new();
    public readonly ReactiveProperty<ITool> ActiveTool = new(null);
    public List<T> GetAllTools<T>() => ToolButtonGroup.GetButtons().Where(b => b.IsVisible()).Cast<T>().ToList();
    
    public ToolButtonPanel()
    {
        ToolButtonGroup.Pressed += button =>
        {
            var tool = (ITool)button;
            ActiveTool.Value?.OnDeactivate();
            ActiveTool.Value = tool;
            tool.OnActivate();
        };
    }
    
    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            var button = (Button)child;
            button.ButtonGroup = ToolButtonGroup;
        }
    }
}
