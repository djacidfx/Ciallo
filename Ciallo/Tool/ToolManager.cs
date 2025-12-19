using System;
using System.Collections.Generic;
using Godot;
using R3;

namespace Ciallo.Tool;

public partial class ToolManager
{
    public readonly Dictionary<BaseButton, ITool> ToolButtonMap = new();
    public ICollection<ITool> Tools => ToolButtonMap.Values;
    public readonly ReactiveProperty<ITool> ActiveTool = new(null);

    public void ActivatePaintTool()
    {
        throw new NotImplementedException();
    }

    public void DeactivateTool()
    {
        ActiveTool.Value?.OnDeactivate();
        ActiveTool.Value = null;
    }
}