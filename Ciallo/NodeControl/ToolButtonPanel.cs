using System.Collections.Generic;
using System.Linq;
using Ciallo.Tool;
using Frent;
using Godot;
using R3;

public partial class ToolButtonPanel : Container
{
    // Current design mix tool button GUI and tool logic data, for being lazy
    public ButtonGroup ToolButtonGroup { get; } = new();
    public readonly ReactiveProperty<ITool> ActiveTool = new(null);
    public List<T> GetAllTools<T>() => ToolButtonGroup.GetButtons().Where(b => b.IsVisible()).Cast<T>().ToList();

    [OnInstantiate]
    public void Initialise(Entity document)
    {
        ToolButtonGroup.Pressed += button =>
        {
            var tool = (ITool)button;
            ActiveTool.Value?.OnDeactivate();
            ActiveTool.Value = tool;
            tool.OnActivate();
        };

        // Button group
        foreach (var child in GetChildren())
        {
            var button = (Button)child;
            button.ButtonGroup = ToolButtonGroup;
        }

        // Assign document
        foreach (var child in GetChildren())
        {
            if (child is not ToolButtonBase button) continue;
            button.Document = document;
        }
    }

    public void DeactivateTool()
    {
        var tool = (ITool)ToolButtonGroup.GetPressedButton();
        tool?.OnDeactivate();
        ToolButtonGroup.GetPressedButton().SetPressed(false);
        ActiveTool.Value = null;
    }
}