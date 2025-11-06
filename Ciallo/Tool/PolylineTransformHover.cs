using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PolylineTransformHover : HoverBase
{
    public Entity HoveredPolyline;
    public CursorDetectionArea RotationArea;
    public CursorDetectionArea[] CornerAreas = [];

    private IDisposable _hoverSub;
    private Entity _layerE;
    private TransformOverlayBox _transformBox;
    private Entity _polylineE;
    private Node2D _midAxis;

    public override void Start()
    {
        // Polyline transform
        if (SelectionManager.SelectedPolylines.Count > 0)
        {
            var worldOverlay = Document.Get<WorldOverlay>();
            // Support single selection currently
            _polylineE = SelectionManager.SelectedPolylines[0];
            if (_polylineE.IsDeletedOrNull())
            {
                SelectionManager.SelectedPolylines.Clear();
            }
            else
            {
                // mid axis
                var centerline = _polylineE.Get<PolylineWireframe>();
                _midAxis = (Node2D)centerline.Duplicate(0); // Don't duplicate script, or constructor will be called.
                worldOverlay.AddChild(_midAxis);
                _midAxis.Visible = true;

                // transform box overlay
                var geom = _polylineE.Get<PolylineGeometry>();
                var rect = geom.Positions.GetBoundingBox();
                _transformBox = new TransformOverlayBox(rect.Size, rect.GetCenter());
                worldOverlay.AddChild(_transformBox);

                // transform cursor area
                var worldArea = Document.Get<WorldCursorDetectionArea>();
                CursorDetectionArea[] areas = worldArea.CreateAddTransformAreas(rect.Size, rect.GetCenter());
                RotationArea = areas[0];
                areas[1].QueueFree();
                CornerAreas = areas[2..6];
            }
        }

        _layerE = SelectionManager.WorkingLayer.Value;
        // Enable cursor detections on polyline
        var holder = _layerE.Get<PolylineAreaHolder>();
        holder.ProcessMode = Node.ProcessModeEnum.Inherit;
        holder.SetAreaCursor(Control.CursorShape.Move);

        // hover hinter
        _hoverSub = Document.Get<WorldCursorDetectionArea>().HoveringArea.Skip(1).Subscribe(area =>
        {
            if (!HoveredPolyline.IsDeletedOrNull()) HoveredPolyline.Get<PolylineWireframe>().SetVisible(false);
            if (area == null)
            {
                HoveredPolyline = Entity.Null;
                return;
            }
            HoveredPolyline = area.SelfEntity;
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<PolylineWireframe>().SetVisible(true);
        });
    }

    public override void End()
    {
        _hoverSub.Dispose();

        // cursor areas
        RotationArea?.QueueFree();
        RotationArea = null;
        Array.ForEach(CornerAreas, b => b.QueueFree());
        CornerAreas = [];
        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;

        // overlays
        if (!HoveredPolyline.IsDeletedOrNull()) HoveredPolyline.Get<PolylineWireframe>().SetVisible(false);
        _midAxis?.QueueFree();
        _midAxis = null;
        _transformBox?.QueueFree();
        _transformBox = null;

        // _layerE = Entity.Null; // Don't clear entity, transform interactor need this value.
    }
}