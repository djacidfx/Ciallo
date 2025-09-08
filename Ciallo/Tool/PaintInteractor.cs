using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PaintInteractor : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer;
            return l != Entity.Null && l.Has<StrokeLayerSetting>();
        }
    }
    
    private StrokeView _strokePreview;
    private readonly List<Vector2> _points = new(){Capacity = 2048};
    private readonly List<float> _radii = new(){Capacity = 2048};

    public override void Start(CursorButtonData data)
    {
        // Shen: I guess this will improve graphics responsiveness
        OS.LowProcessorUsageMode = false;
        
        _strokePreview = new StrokeView();
        var layerE = SelectionManager.WorkingLayer;
        var layerView = layerE.Get<StrokeLayerView>();
        layerView.AddChild(_strokePreview);
        
        _points.Add(data.WorldPosition);
        _radii.Add(2f); // TODO: Brush size setting
        _strokePreview.UpdateStroke(_points, _radii);
    }

    public override void Interacting(CursorMotionData data)
    {
        _points.Add(data.WorldPosition);
        _radii.Add(Mathf.Lerp(2f, 8f, data.Pressure));
        _strokePreview.UpdateStroke(_points, _radii);
    }

    public override void End(CursorButtonData data)
    {
        var parentPath = SelectionManager.WorkingLayerPath;
        var parentE = SelectionManager.WorkingLayer;
        ImmutableArray<int> path = [..parentPath, parentE.Get<LayerTreeNode>().ChildCount];
        new NewStrokeCmd(path)
            .Combine(new ResetStrokeGeometryCmd(path, _points, _radii)).Commit();
        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        _points.Clear();
        _radii.Clear();
        _strokePreview.QueueFree();
        OS.LowProcessorUsageMode = true;
    }
}