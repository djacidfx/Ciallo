using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Godot;
using R3;

namespace Ciallo.Tool;

public static class ToolManager
{
    public static ButtonGroup ToolButtonGroup { get; } = new();
    public static readonly ReactiveProperty<ITool> ActiveTool = new(null);
    
    public static List<T> GetAllTools<T>() => ToolButtonGroup.GetButtons().Cast<T>().ToList();

    static ToolManager()
    {
        ToolButtonGroup.Pressed += button => ActiveTool.Value = (ITool)button;
    }
}