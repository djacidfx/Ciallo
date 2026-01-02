using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PolylineTransformHover : InteractiveSessionBase
{
    public Entity HoveredPolyline;
    public Body RotationArea;
    public Body[] CornerAreas = [];
    public bool CanTransform
    {
        get
        {
            bool polylineHovered = !HoveredPolyline.IsNull;
            bool rotationDotHovered = RotationArea?.IsHovered == true;
            bool cornerDotsHovered = CornerAreas.Any(a => a.IsHovered);
            return polylineHovered || rotationDotHovered || cornerDotsHovered;
        }
    }

    private IDisposable _hoverSub;
    private Entity _layerE;
    private TransformOverlayBox _transformBox;
    private List<Entity> _polylineEs;
    private readonly List<Node2D> _wireframes = [];

    public override void Start(CursorButtonData data)
    {
        var selectionManager = Document.Get<SelectionManager>();
        // Polyline transform
        if (selectionManager.SelectedPolylines.Count > 0)
        {
            var worldOverlay = Document.Get<WorldOverlay>();

            _polylineEs = [..selectionManager.SelectedPolylines];

            // transform box
            Rect2 rect = default;
            foreach (var (i, e) in _polylineEs.Index())
            {
                var wire = (Node2D)e.Get<PolylineWireframe>().Duplicate(0); // 0 means avoid duplicating script. Script duplication call constructor.
                worldOverlay.AddChild(wire);
                wire.Visible = true;
                _wireframes.Add(wire);

                // transform box overlay
                var geom = e.Get<PolylineGeometry>();
                var bound = geom.Positions.GetBoundingBox();
                rect = i == 0 ? bound : rect.Merge(bound);
            }
            if (!rect.IsEqualApprox(default))
            {
                _transformBox = new TransformOverlayBox(rect.Size, rect.GetCenter());
                worldOverlay.AddChild(_transformBox);
            }

            // transform cursor area
            var worldArea = Document.Get<WorldBody>();
            Body[] areas = worldArea.CreateAddTransformAreas(rect.Size, rect.GetCenter());
            RotationArea = areas[0];
            areas[1].QueueFree();
            CornerAreas = areas[2..6];
        }

        // Enable cursor detections on polylines of working layer
        _layerE = selectionManager.WorkingLayer.Value;
        var holder = _layerE.Get<PolylineBodyHolder>();
        holder.SetAreaCursor(Control.CursorShape.Move);

        // hover hinter
        _hoverSub = Document.Get<WorldBody>().HoveringArea.Skip(1).Subscribe(area =>
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
    public override void Interacting(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        _hoverSub.Dispose();

        // cursor areas
        RotationArea?.QueueFree();
        RotationArea = null;
        Array.ForEach(CornerAreas, b => b.QueueFree());
        CornerAreas = [];

        _layerE.Get<PolylineBodyHolder>().SetAreaCursor(Control.CursorShape.Arrow);

        // overlays
        if (!HoveredPolyline.IsDeletedOrNull()) HoveredPolyline.Get<PolylineWireframe>().SetVisible(false);
        _wireframes.ForEach(node => node.QueueFree());
        _wireframes.Clear();
        _transformBox?.QueueFree();
        _transformBox = null;

        HoveredPolyline = Entity.Null;
    }
    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;
}