using Arch.Core;
using Godot;

namespace Ciallo.Tool;

public static class ToolManager
{
    public static ButtonGroup ToolButtonGroup { get; } = new();

    public static ITool GetActiveTool() => (ITool)ToolButtonGroup.GetPressedButton();
}