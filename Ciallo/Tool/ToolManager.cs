using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Frent.Components;
using R3;

namespace Ciallo.Tool;

public partial class ToolManager : IInitable
{
    public Dictionary<ToolButton, List<ITool>> ToolButtonMap;
    public IEnumerable<ITool> Tools => ToolButtonMap.Values.SelectMany(list => list);
    public ToolButton? ActiveToolButton = null;
    public readonly ReactiveProperty<ITool> ActiveTool = new(null);
    public Entity Document;

    public void Init(Entity self)
    {
        Document = self;
        ToolButtonMap = InitializeToolButtonMap(self);
    }

    public void ActivatePaintTool()
    {
        Document.Get<ToolButtonPanel>().PressButton(ToolButton.Paint);
    }

    public void DeactivateTool()
    {
        ActiveTool.Value?.OnDeactivate();
        ActiveTool.Value = null;
    }

    public void OnSwitchToolButton(ToolButton? toolButton)
    {
        ActiveToolButton = toolButton;
        ActiveTool.Value?.OnDeactivate();
        ActiveTool.Value = null;
        if (toolButton == null)
            return;
        ToolButtonMap.TryGetValue(toolButton.Value, out var tools);
        if (tools == null || tools.Count == 0)
            return;
        var selectionManager = Document.Get<SelectionManager>();
        foreach (var tool in tools)
        {
            if (tool.CanHandleLayer([selectionManager.WorkingLayer.Value]))
            {
                ActiveTool.Value = tool;
                ActiveTool.Value.OnActivate([selectionManager.WorkingLayer.Value]);
                return;
            }
        }
    }

    public void OnSwitchLayer(params Entity[] layerEs)
    {
        ActiveTool.Value?.OnDeactivate();
        ActiveTool.Value = null;
        if (ActiveToolButton == null)
            return;
        ToolButtonMap.TryGetValue(ActiveToolButton.Value, out var tools);
        if (tools == null || tools.Count == 0)
            return;
        foreach (var tool in tools)
        {
            if (tool.CanHandleLayer(layerEs))
            {
                ActiveTool.Value = tool;
                ActiveTool.Value.OnActivate(layerEs);
                return;
            }
        }
    }
}