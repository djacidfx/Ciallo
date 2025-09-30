using System.Collections.Generic;
using System.Linq;
using Godot;
using R3;

namespace Ciallo.Tool;

public static class AppToolManager
{
    public static ButtonGroup ToolButtonGroup { get; } = new();
    public static readonly ReactiveProperty<ITool> ActiveTool = new(null);
    
    public static List<T> GetAllTools<T>() => ToolButtonGroup.GetButtons().Cast<T>().ToList();

    static AppToolManager()
    {
        ToolButtonGroup.Pressed += button => ActiveTool.Value = (ITool)button;
    }
}