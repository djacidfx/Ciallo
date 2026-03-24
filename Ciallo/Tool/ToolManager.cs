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
    public ReactiveProperty<ToolButton?> PressedToolButton => AppPreference.PressedToolButton;
    public ReadOnlyReactiveProperty<ITool> WorkingTool { get; private set; }
    public Entity Document;

    public void Init(Entity self)
    {
        Document = self;
        ToolButtonMap = InitializeToolButtonMap(self);
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer;
        WorkingTool = workingLayer.CombineLatest(PressedToolButton, (layerE, toolButton) =>
        {
            if (toolButton == null)
                return null;
            ToolButtonMap.TryGetValue(toolButton.Value, out var tools);
            if (tools == null || tools.Count == 0)
                return null;
            return tools.FirstOrDefault(tool => tool.CanHandleLayer(layerE));
        }).ToReadOnlyReactiveProperty();

        WorkingTool.Pairwise().Subscribe(tool =>
        {
            tool.Previous?.OnDeactivate();
            tool.Current?.OnActivate(Document.Get<SelectionManager>().WorkingLayer.Value);
        });
    }

    public void ActivatePaintTool()
    {
        PressedToolButton.Value = ToolButton.Paint;
    }
}