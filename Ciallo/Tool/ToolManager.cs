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
    public Dictionary<ToolButton, List<ITool>> ToolButtonMap; // Init by source generation
    public IEnumerable<ITool> Tools => ToolButtonMap.Values.SelectMany(list => list);
    public ReactiveProperty<ToolButton?> PressedToolButton => AppPreference.PressedToolButton;
    public ReactiveProperty<ITool> WorkingTool = new(null);
    public Entity Document;
    private readonly ReactiveProperty<bool> _isRollingFrame = new(false);
    private int _rollingFrameScopeCount;

    public void Init(Entity self)
    {
        Document = self;
        ToolButtonMap = InitializeToolButtonMap(self);
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer;
        // Switch tool
        _isRollingFrame
            .CombineLatest(workingLayer, ValueTuple.Create)
            .CombineLatest(PressedToolButton, ValueTuple.Create)
            .Subscribe(tuple =>
            {
                var (isRollingFrame, layerE) = tuple.Item1;
                var toolButton = tuple.Item2;
                var targetTool = isRollingFrame ? null : ResolveTool(layerE, toolButton);
                SwitchWorkingTool(targetTool, layerE);
            }).AddTo(Document);
    }

    public void ObserveTimelineRolling(Observable<bool> isTimelineRolling)
    {
        IDisposable rollingFrameScope = null;
        isTimelineRolling.Subscribe(rolling =>
        {
            if (rolling)
            {
                rollingFrameScope ??= BeginRollingFrame();
                return;
            }

            rollingFrameScope?.Dispose();
            rollingFrameScope = null;
        }).AddTo(Document);
    }

    public IDisposable BeginRollingFrame()
    {
        _rollingFrameScopeCount++;
        if (_rollingFrameScopeCount == 1)
            _isRollingFrame.Value = true;

        return new RollingFrameScope(this);
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

    private ITool ResolveTool(Entity layerE, ToolButton? toolButton) =>
        layerE.IsNull || toolButton == null
            ? null
            : ToolButtonMap[toolButton.Value].FirstOrDefault(t => t.CanHandleLayer(layerE));

    private void SwitchWorkingTool(ITool targetTool, Entity layerE)
    {
        WorkingTool.Value?.OnDeactivate();
        targetTool?.OnActivate(layerE);
        WorkingTool.Value = targetTool;
    }

    private void EndRollingFrame()
    {
        _rollingFrameScopeCount--;
        if (_rollingFrameScopeCount == 0)
            _isRollingFrame.Value = false;
    }

    private sealed class RollingFrameScope : IDisposable
    {
        private ToolManager _manager;

        public RollingFrameScope(ToolManager manager)
        {
            _manager = manager;
        }

        public void Dispose()
        {
            _manager.EndRollingFrame();
            _manager = null;
        }
    }
}
