using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Frent.Components;
using R3;

namespace Ciallo.Tool;

public partial class ToolManager : IInitable, IDestroyable
{
    public Dictionary<ToolButton, List<ITool>> ToolButtonMap;
    public IEnumerable<ITool> Tools => ToolButtonMap.Values.SelectMany(list => list);
    public ReactiveProperty<ToolButton?> PressedToolButton => AppPreference.PressedToolButton;
    public ReactiveProperty<ITool> WorkingTool = new(null);
    public Entity Document;

    public void Init(Entity self)
    {
        Document = self;
        ToolButtonMap = InitializeToolButtonMap(self);
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer;
        workingLayer.CombineLatest(PressedToolButton, ValueTuple.Create)
            .DelayFrame(1)
            .Subscribe(tuple =>
            {
                var (layerE, toolButton) = tuple;
                if (toolButton == null)
                {
                    WorkingTool.Value = null;
                    return;
                }

                var targetTool = layerE.IsNull
                    ? null
                    : ToolButtonMap[toolButton.Value].FirstOrDefault(t => t.CanHandleLayer(layerE));
                WorkingTool.Value?.OnDeactivate();
                targetTool?.OnActivate(layerE);
                WorkingTool.Value = targetTool;
            }).AddTo(Document);
    }

    public void Destroy() => DeactivateWorkingTool();

    public void DeactivateWorkingTool()
    {
        WorkingTool.Value?.OnDeactivate();
        WorkingTool.Value = null;
    }

    public void ActivatePaintTool()
    {
        PressedToolButton.Value = ToolButton.Paint;
    }
}