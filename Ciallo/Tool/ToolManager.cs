using System;
using System.Collections.Generic;
using R3;

namespace Ciallo.Tool;

public class ToolManager
{
    public List<ITool> Tools = [];
    public ReactiveProperty<ITool> ActiveTool = new(null);

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