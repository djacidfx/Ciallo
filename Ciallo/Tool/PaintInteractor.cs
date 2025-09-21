using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PaintInteractor : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer;
            return l != Entity.Null && l.Has<PolylineLayerSetting>();
        }
    }
    
    private StrokeView _strokePreview;
    private readonly List<Vector2> _points = new(){Capacity = 2048};
    private readonly List<float> _radii = new(){Capacity = 2048};
    
    private Vector2 _lastScreenPoint;
    private Vector2 _lastDirection;
    private readonly float _minDistance = 7f; // in pixel
    private readonly float _maxDistance = 15f; // in pixel
    private readonly float _minCosAngle = Mathf.Cos(Mathf.DegToRad(5f));

    public override void Start(CursorButtonData data)
    {
        // Shen: I guess this will improve graphics responsiveness
        OS.LowProcessorUsageMode = false;
        
        _strokePreview = new StrokeView();
        var layerE = SelectionManager.WorkingLayer;
        var layerView = layerE.Get<PolylineLayerView>();
        layerView.AddChild(_strokePreview);
        
        _points.Add(data.WorldPosition);
        _radii.Add(2f);
        _lastScreenPoint = data.ScreenPosition;
        _lastDirection = Vector2.FromAngle(0);
        _strokePreview.SetGeometry(_points, _radii);
    }
    
    public override void Interacting(CursorMotionData data)
    {
        bool isSmaller = data.ScreenPosition.DistanceTo(_lastScreenPoint) < _minDistance;
        bool isLarger = data.ScreenPosition.DistanceTo(_lastScreenPoint) > _maxDistance;
        bool isWinding = data.ScreenPosition.DirectionTo(_lastScreenPoint).Dot(_lastDirection) < _minCosAngle;
        if (isSmaller) return;
        if (!isLarger && !isWinding) return;
        _points.Add(data.WorldPosition);
        _radii.Add(Mathf.Lerp(2f, 6f, data.Pressure));

        // Very basic smoothing
        float delta = 0.1f;
        for(int i = 0; i < 4; i++)
        {
            int idx = _points.Count - 1 - i;
            if (idx < 1) break;
            _radii[idx] = Mathf.Lerp(_radii[idx], _radii[idx - 1], delta);
            _points[idx] = _points[idx].Lerp(_points[idx - 1], delta);
        }
        
        _strokePreview.SetGeometry(_points, _radii);
        _lastDirection = data.ScreenPosition.DirectionTo(_lastScreenPoint).Normalized();
        _lastScreenPoint = data.ScreenPosition;
    }

    public override void End(CursorButtonData data)
    {
        var parentPath = SelectionManager.WorkingLayerPath;
        var parentE = SelectionManager.WorkingLayer;
        ImmutableArray<int> path = [..parentPath, parentE.Get<LayerTreeNode>().ChildCount];
        new NewStrokeCmd(path)
            .Combine(new SetStrokeGeometryCmd(path, _points, _radii)).Commit();
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