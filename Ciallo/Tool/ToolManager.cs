using Arch.Core;
using Godot;

namespace Ciallo.Tool;

public static class ToolManager
{
    public static ButtonGroup ToolButtonGroup { get; } = new();

    public static ITool GetActiveTool() => ToolButtonGroup.GetPressedButton() as ITool;
    
    public static void RegisterDocument(Entity document)
    {
        foreach (var button in ToolButtonGroup.GetButtons())
        {
            var tool = (ITool)button;
            tool.OnRegisterDocument(document);
        }
    }
}