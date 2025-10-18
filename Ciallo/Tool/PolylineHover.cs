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

            var geom = _polylineE.Get<StrokeGeometry>();
            var boundingRect = geom.Points.GetBoundingBox();
            _transformBox = new TransformBox(boundingRect.Size, boundingRect.GetCenter());
            worldOverlay.AddChild(_transformBox);
        }

        _hoverSub = Document.Get<WorldCursorDetectionArea>().HoveringArea.Skip(1).Subscribe(area =>
        {
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(false);
            if (area == null)
            {
                HoveredPolyline = Entity.Null;
                return;
            }
            HoveredPolyline = area.SelfEntity;
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(true);
        });
    }

    public override void Cancel()
    {
        _hoverSub.Dispose();
        if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(false);

        _transformBox?.QueueFree();
        _transformBox = null;
        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}