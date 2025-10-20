using System;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformInteractor(PolylineHover hover) : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
            bool translationHovered = !hover.HoveredPolyline.IsNull;
            bool rotationHovered = hover.RotationArea?.IsHovered == true;
            bool cornerHovered = hover.CornerAreas.Any(a => a.IsHovered);

            return hasSelection || translationHovered || rotationHovered || cornerHovered;
        }
    }
    private int _transformType = -1; // -1: Deselect, 0: Move, 1: Rotate, 2~5: Scale corners
    private Entity _polylineE;

    private Transform2D _currTransform;
    private Vector2[] _startCorners;
    private Rect2 _origRect;
    private TransformOverlayBox _transformBox;

    public override void Prepare(CursorButtonData data)
    {
        bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
        bool translationHovered = !hover.HoveredPolyline.IsNull;
        bool rotationHovered = hover.RotationArea?.IsHovered == true;
        bool cornerHovered = hover.CornerAreas.Any(a => a.IsHovered);

        if (translationHovered)
        {
            _transformType = 0;
        }
        else if (rotationHovered)
        {
            _transformType = 1;
        }
        else if (cornerHovered)
        {
            _transformType = Array.FindIndex(hover.CornerAreas, a => a.IsHovered) + 2;
        }
        else if (hasSelection) _transformType = -1;
    }

    public override void Start(CursorButtonData data)
    {
        if (_transformType == -1)
        {
            SelectionManager.SelectedPolylines.Clear();
        }
        else if (_transformType == 0)
        {
            _polylineE = hover.HoveredPolyline;
        }
        else if (_transformType == 1)
        {
            _polylineE = SelectionManager.SelectedPolylines[0];
        }
        else if (_transformType > 1)
        {
            _polylineE = SelectionManager.SelectedPolylines[0];
            _currTransform = Transform2D.Identity;

            var geom = _polylineE.Get<PolylineGeometry>();
            _origRect = geom.Points.GetBoundingBox();
            var center = _origRect.GetCenter();
            var half = _origRect.Size * 0.5f;
            _startCorners =
            [
                center - half, // -half
                new(center.X - half.X, center.Y + half.Y),
                center + half,
                new(center.X + half.X, center.Y - half.Y),
            ];

            _transformBox = new TransformOverlayBox(_origRect.Size, _origRect.GetCenter());
            Document.Get<WorldOverlay>().AddChild(_transformBox);
        }
    }
    public override void Interacting(CursorMotionData data)
    {
        if (_transformType == 0)
            _polylineE.Get<StrokeView>().Translate(data.WorldDelta);

        if (_transformType == 1)
        {
            var strokeView = _polylineE.Get<StrokeView>();
            var center = _polylineE.Get<PolylineGeometry>().Points.GetBoundingBox().GetCenter();
            var angleDelta = (data.PrevWorldPosition - center).AngleTo(data.WorldPosition - center);
            strokeView.Transform = strokeView.GetTransform()
                .Translated(-center).Rotated(angleDelta).Translated(center);
        }

        // Scale, gen by copilot
        if (_transformType > 1)
        {
            bool fixRatio = Input.IsKeyPressed(Key.Shift);
            bool fixCenter = Input.IsKeyPressed(Key.Alt);

            int cornerIndex = _transformType - 2;
            int oppositeIndex = (cornerIndex + 2) & 3;

            // World-axis-aligned basis
            var axisXDir = Vector2.Right;
            var axisYDir = Vector2.Down;

            var origCenter = _origRect.GetCenter();
            var pivot = fixCenter ? origCenter : _startCorners[oppositeIndex];

            // Vectors from pivot to original dragged corner and current pointer
            var v0 = _startCorners[cornerIndex] - pivot;
            var v = data.WorldPosition - pivot;

            // Per-axis scales from pivot
            float sX = v.Dot(axisXDir) / v0.Dot(axisXDir);
            float sY = v.Dot(axisYDir) / v0.Dot(axisYDir);

            if (fixRatio)
            {
                float u = Mathf.Max(Mathf.Abs(sX), Mathf.Abs(sY));
                sX = Mathf.Sign(sX) * u;
                sY = Mathf.Sign(sY) * u;
            }

            var newX = axisXDir * sX;
            var newY = axisYDir * sY;

            // Build S and translate so that pivot remains fixed: T(p) = S*p + (pivot - S*pivot)
            var t = new Transform2D(newX, newY, Vector2.Zero);
            var origin = pivot - (t * pivot);
            _currTransform = new Transform2D(newX, newY, origin);

            var geom = _polylineE.Get<PolylineGeometry>();
            var points = geom.Points.Select(p => _currTransform * p).ToArray();
            _polylineE.Get<StrokeView>().SetGeometry(points, geom.Radii);
            ;
            _transformBox.UpdateGeometry(_currTransform * _origRect);
        }
    }

    public override void End(CursorButtonData data)
    {
        if (_transformType == -1) return;
        var resultT = _transformType is 0 or 1 ? _polylineE.Get<StrokeView>().GetTransform() : _currTransform;

        SelectionManager.SelectedPolylines.Clear();
        SelectionManager.SelectedPolylines.Add(_polylineE);

        if (!resultT.IsEqualApprox(Transform2D.Identity))
        {
            var newGeom = _polylineE.Get<PolylineGeometry>().Clone();
            newGeom.Points = newGeom.Points.Select(p => resultT * p).ToList();
            new SetStrokeGeometryCmd(_polylineE, newGeom).Commit();
        }

        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        if (_transformType is 0 or 1)
        {
            var strokeView = _polylineE.Get<StrokeView>();
            strokeView.Transform = Transform2D.Identity;
        }

        if (_transformType > 1)
        {
            _transformBox.QueueFree();
            _currTransform = Transform2D.Identity;
            _polylineE.Get<StrokeView>().Visible = true;
        }

        _transformType = -1;
        _polylineE = Entity.Null;
    }
}