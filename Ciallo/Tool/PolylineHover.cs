using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PolylineHover : HoverBase
{
    public Entity HoveredPolyline;

    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return !layerE.IsNull && layerE.Has<PolylineLayerSetting>();
        }
    }

    private IDisposable _hoverSub;
    private Entity _layerE;
    private TransformBox _transformBox;
    private Entity _polylineE;
    private Node2D _midAxis;

    public override void Start(CursorMotionData data)
    {
        _layerE = SelectionManager.WorkingLayer.Value;
        // Enable cursor detections on polyline
        var holder = _layerE.Get<PolylineAreaHolder>();
        holder.ProcessMode = Node.ProcessModeEnum.Inherit;
        holder.SetAreaCursor(Control.CursorShape.Move);
        // Polyline transform
        if (SelectionManager.SelectedPolylines.Count > 0)
        {
            var worldOverlay = Document.Get<WorldOverlay>();
            // Support single selection currently
            _polylineE = SelectionManager.SelectedPolylines[0];

            // mid axis
            var centerline = _polylineE.Get<StrokeCenterline>();
            _midAxis = (Node2D)centerline.Duplicate(0); // Don't duplicate script, or constructor will be called.
            worldOverlay.AddChild(_midAxis);
            _midAxis.Visible = true;

            // transform box
            var geom = _polylineE.Get<StrokeGeometry>();
            var boundingRect = geom.Points.GetBoundingBox();
            _transformBox = new TransformBox(boundingRect.Size, boundingRect.GetCenter());
            worldOverlay.AddChild(_transformBox);
        }

        // hover hinter
        _hoverSub = Document.Get<WorldCursorDetectionArea>().HoveringArea.Skip(1).Subscribe(area =>
        {
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeCenterline>().SetVisible(false);
            if (area == null)
            {
                HoveredPolyline = Entity.Null;
                return;
            }
            HoveredPolyline = area.SelfEntity;
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeCenterline>().SetVisible(true);
        });
    }

    public override void Cancel()
    {
        _hoverSub.Dispose();
        if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeCenterline>().SetVisible(false);

        _midAxis?.QueueFree();
        _midAxis = null;
        _transformBox?.QueueFree();
        _transformBox = null;
        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}