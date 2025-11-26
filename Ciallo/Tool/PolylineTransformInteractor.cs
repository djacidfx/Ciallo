using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformInteractor(PolylineTransformHover hover) : InteractorBase
{
    private int _transformType = -1; // -1: Rect selection/Deselect, 0: Move, 1: Rotate, 2~5: Scale corners

    private Transform2D _currTransform = Transform2D.Identity;
    private Vector2[] _startCorners;
    private Rect2 _origRect;
    private TransformOverlayBox _transformBox;
    private Vector2 _center;
    private List<Entity> _processingEs;

    public override bool Prepare(CursorButtonData data)
    {
        bool polylineHovered = !hover.HoveredPolyline.IsNull;
        bool rotationDotHovered = hover.RotationArea?.IsHovered == true;
        bool cornerDotsHovered = hover.CornerAreas.Any(a => a.IsHovered);

        if (polylineHovered && Input.IsKeyPressed(Key.Shift))
        {
            var hoverE = hover.HoveredPolyline;
            if (SelectionManager.SelectedPolylines.Remove(hoverE)) return false;
            SelectionManager.SelectedPolylines.Add(hoverE);
            _transformType = 0;
            return true;
        }
        if (polylineHovered)
        {
            var hoverE = hover.HoveredPolyline;
            if (!SelectionManager.SelectedPolylines.Contains(hoverE))
            {
                SelectionManager.SelectedPolylines.Clear();
                SelectionManager.SelectedPolylines.Add(hoverE);
            }
            _transformType = 0;
            return true;
        }
        if (rotationDotHovered)
        {
            _transformType = 1;
            return true;
        }
        if (cornerDotsHovered)
        {
            _transformType = Array.FindIndex(hover.CornerAreas, a => a.IsHovered) + 2;
            return true;
        }
        _transformType = -1;
        return true;
    }

    public override void Start(CursorButtonData data)
    {
        if (_transformType == -1) SelectionManager.SelectedPolylines.Clear();

        _processingEs = SelectionManager.SelectedPolylines.ToList();

        if (_transformType >= 1)
        {
            _currTransform = Transform2D.Identity;

            foreach (var (i, e) in _processingEs.Index())
            {
                var geom = e.Get<PolylineGeometry>();
                var bounding = geom.Positions.GetBoundingBox();
                _origRect = i == 0 ? bounding : _origRect.Merge(bounding);
            }

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

            _transformBox.UpdateGeometry(_currTransform * _origRect);
        }

        // Update view
        foreach (var e in _processingEs)
        {
            var geom = e.Get<PolylineGeometry>();
            var points = geom.Positions.Select(p => _currTransform * p).ToArray();
            if (e.Has<StrokeSetting>())
            {
                e.Get<StrokeView>().SetGeometry(points, geom.Radii, geom.Pressures);
            }
            if (e.Has<FilledPolygonSetting>())
            {
                e.Get<Polygon2D>().SetPolygon(points);
            }
        }
    }

    public override void End(CursorButtonData data)
    {
        if (_transformType == -1) return;
        var resultT = _currTransform;

        if (!resultT.IsEqualApprox(Transform2D.Identity))
        {
            foreach (var e in _processingEs)
            {
                var newGeom = e.Get<PolylineGeometry>().Clone();
                newGeom.Positions = newGeom.Positions.Select(p => resultT * p).ToList();
                new SetPolylineGeometryCmd(e, newGeom).Commit();
            }
        }

        Clear();
    }

    public override void Cancel()
    {
        // Clean up view change
        foreach (var e in _processingEs)
        {
            var geom = e.Get<PolylineGeometry>();
            var points = geom.Positions.ToArray();
            if (e.Has<StrokeSetting>())
            {
                e.Get<StrokeView>().SetGeometry(points, geom.Radii, geom.Pressures);
            }
            if (e.Has<FilledPolygonSetting>())
            {
                e.Get<Polygon2D>().SetPolygon(points);
            }
        }
        Clear();
    }

    public void Clear()
    {
        if (_transformType > 1) _transformBox.QueueFree();
        _currTransform = Transform2D.Identity;
        _transformType = -1;
        _processingEs = null;
    }
}