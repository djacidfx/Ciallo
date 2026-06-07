using System;
using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class TrimInteractor : InteractiveSessionBase
{
    private readonly List<Vector2> _gesture = [];
    private readonly List<StrokeView> _previewViews = [];
    private StrokeView _gestureView;
    private TrimPlan _latestPlan;

    public new TrimTool Tool => (TrimTool)base.Tool;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        _gesture.Clear();
        _gesture.Add(data.WorldPosition);

        _gestureView = new StrokeView { Material = AutoloadRendering.DashWireframeMaterial };
        Document.Get<WorldOverlay>().AddChild(_gestureView);

        UpdatePreview();
    }

    public override void Moving(CursorMotionData data)
    {
        _gesture.Add(data.WorldPosition);
        UpdatePreview();
    }

    public override void End(CursorButtonData data)
    {
        _latestPlan?.Commit(Document);
        Clear();
    }

    public override void Cancel() => Clear();

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    private void UpdatePreview()
    {
        _gestureView.SetGeometry(_gesture, AppPreference.StrokeWireframeRadius);
        _latestPlan = TrimPlan.Create(Cache, [.. _gesture]);
        SetPreviewPlan(_latestPlan);
    }

    private void SetPreviewPlan(TrimPlan plan)
    {
        ClearPreview();
        foreach (var segment in plan.HighlightSegments)
        {
            var view = new StrokeView { Material = AutoloadRendering.MissingStrokeBrushMaterial };
            Document.Get<WorldOverlay>().AddChild(view);
            view.SetGeometry(segment.Points, segment.Radius);
            _previewViews.Add(view);
        }
    }

    private void ClearPreview()
    {
        foreach (var view in _previewViews)
            view.QueueFree();
        _previewViews.Clear();
    }

    private void Clear()
    {
        _gesture.Clear();
        _latestPlan = null;
        _gestureView?.QueueFree();
        _gestureView = null;
        ClearPreview();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
