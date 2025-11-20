using System;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformInteractor(PolylineTransformHover hover) : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
            bool polylineHovered = !hover.HoveredPolyline.IsNull;
            bool rotationDotHovered = hover.RotationArea?.IsHovered == true;
            bool cornerDotsHovered = hover.CornerAreas.Any(a => a.IsHovered);

            return hasSelection || polylineHovered || rotationDotHovered || cornerDotsHovered;
        }
    }
    private int _transformType = -1; // -1: Deselect, 0: Move, 1: Rotate, 2~5: Scale corners
    private Entity _polylineE;
    private int _objectType = -1; // 0: stroke, 1: filled polygon

    private Transform2D _currTransform = Transform2D.Identity;
    private Vector2[] _startCorners;
    private Rect2 _origRect;
    private TransformOverlayBox _transformBox;
    private Vector2 _center;

    public override void Prepare(CursorButtonData data)
    {
        bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
        bool polylineHovered = !hover.HoveredPolyline.IsNull;
        bool rotationDotHovered = hover.RotationArea?.IsHovered == true;
        bool cornerDotsHovered = hover.CornerAreas.Any(a => a.IsHovered);

        if (polylineHovered)
        {
            _transformType = 0;
        }
        else if (rotationDotHovered)
        {
            _transformType = 1;
        }
        else if (cornerDotsHovered)
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
            return;
        }

        if (_transformType == 0)
        {
            _polylineE = hover.HoveredPolyline;
        }
        else if (_transformType >= 1)
        {
            _polylineE = SelectionManager.SelectedPolylines[0];
            _currTransform = Transform2D.Identity;

            var geom = _polylineE.Get<PolylineGeometry>();
            _origRect = geom.Positions.GetBoundingBox();
            // Show transform box only when scaling
            if (_transformType > 1)
            {
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

        if (_polylineE.Has<StrokeView>()) _objectType = 0;
        else if (_polylineE.Has<Polygon2D>()) _objectType = 1;
    }

    public override void Interacting(CursorMotionData data)
    {
        if (_transformType == -1) return;
        // Compute transform
        if (_transformType == 0)
            _currTransform = _currTransform.Translated(data.WorldDelta);

        if (_transformType == 1)
        {
            _center = _origRect.GetCenter();
            var angleDelta = (data.PrevWorldPosition - _center).AngleTo(data.WorldPosition - _center);
            _currTransform = _currTransform.Translated(-_center).Rotated(angleDelta).Translated(_center);
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
            ;
            _transformBox.UpdateGeometry(_currTransform * _origRect);
        }

        // Update view
        var geom = _polylineE.Get<PolylineGeometry>();
        var points = geom.Positions.Select(p => _currTransform * p).ToArray();
        if (_objectType == 0)
        {
            _polylineE.Get<StrokeView>().SetGeometry(points, geom.Radii, geom.Pressures);
        }
        else if (_objectType == 1)
        {
            _polylineE.Get<Polygon2D>().SetPolygon(points);
        }
    }

    public override void End(CursorButtonData data)
    {
        if (_transformType == -1) return;
        var resultT = _currTransform;

        SelectionManager.SelectedPolylines.Clear();
        SelectionManager.SelectedPolylines.Add(_polylineE);

        if (!resultT.IsEqualApprox(Transform2D.Identity))
        {
            var newGeom = _polylineE.Get<PolylineGeometry>().Clone();
            newGeom.Positions = newGeom.Positions.Select(p => resultT * p).ToList();
            new SetPolylineGeometryCmd(_polylineE, newGeom).Commit();
        }

        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        if (_transformType > 1)
        {
            _transformBox.QueueFree();
        }
        _currTransform = Transform2D.Identity;
        _transformType = -1;
        _objectType = -1;
        _polylineE = Entity.Null;
    }
}