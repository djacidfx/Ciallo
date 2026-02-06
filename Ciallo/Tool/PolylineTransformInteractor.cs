using System;
using System.Linq;
using System.Runtime.InteropServices;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformInteractor : InteractiveSessionBase
{
    private int _transformType = -1; // 0: Move, 1: Rotate, 2~5: Scale corners

    private Entity[] _processingEs;
    private Vector2[][] _currPositions;
    private Transform2D _currTransform = Transform2D.Identity;

    private Vector2[] _startCorners;
    private Rect2 _origRect;
    private TransformOverlayBox _transformBox;
    private Vector2 _center;

    public override void BeforeSrcEnd(InteractiveSessionBase session)
    {
        if (session is not PolylineTransformHover hover) return;

        bool polylineHovered = !hover.HoveredPolyline.IsNull;
        bool rotationDotHovered = hover.RotationBody?.IsHovered == true;
        bool cornerDotsHovered = hover.CornerBodies.Any(a => a.IsHovered);

        var selectionManager = Document.Get<SelectionManager>();
        if (polylineHovered && Input.IsKeyPressed(Key.Shift))
        {
            var hoverE = hover.HoveredPolyline;
            if (!selectionManager.SelectedPolylines.Remove(hoverE))
                selectionManager.SelectedPolylines.Add(hoverE);
            _transformType = 0;
        }
        if (polylineHovered)
        {
            var hoverE = hover.HoveredPolyline;
            if (!selectionManager.SelectedPolylines.Contains(hoverE))
            {
                selectionManager.SelectedPolylines.Clear();
                selectionManager.SelectedPolylines.Add(hoverE);
            }
            _transformType = 0;
        }
        if (rotationDotHovered)
        {
            _transformType = 1;
        }
        if (cornerDotsHovered)
        {
            _transformType = Array.FindIndex(hover.CornerBodies, a => a.IsHovered) + 2;
        }
    }

    public override void Start(CursorButtonData data)
    {
        _processingEs = Document.Get<SelectionManager>().SelectedPolylines.ToArray();
        _currTransform = Transform2D.Identity;
        _currPositions = new Vector2[_processingEs.Length][];
        foreach (var (i, e) in _processingEs.Index())
        {
            var geom = e.Get<PolylineGeometry>();
            var bounding = geom.Positions.GetBoundingBox();
            _origRect = i == 0 ? bounding : _origRect.Merge(bounding);
            // allocate buffer once per interaction
            var buffer = new Vector2[geom.Positions.Count];
            for (int j = 0; j < buffer.Length; j++)
            {
                buffer[j] = geom.Positions[j];
            }
            _currPositions[i] = buffer;
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

    public override void Interacting(CursorMotionData data)
    {
        // Note: Still stutter when moving multiple strokes
        // No GC during interaction
        // Godot profiler and Rider's monitor both show no spikes.

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
        foreach (var (i, e) in _processingEs.Index())
        {
            var geom = e.Get<PolylineGeometry>();
            for (int j = 0; j < _currPositions[i].Length; j++)
                _currPositions[i][j] = _currTransform * geom.Positions[j];

            if (e.Has<StrokeSetting>())
            {
                e.Get<StrokeView>().SetGeometry(_currPositions[i], geom.Radii, geom.Pressures);
            }
            if (e.Has<FilledPolygonSetting>())
            {
                e.Get<Polygon2D>().SetPolygon(CollectionsMarshal.AsSpan(_currPositions[i].ToSimplePolygon()));
            }
        }
    }

    public override void End(CursorButtonData data)
    {
        var resultT = _currTransform;
        if (!resultT.IsEqualApprox(Transform2D.Identity))
        {
            var cmd = new CommandBuilder();
            foreach (var e in _processingEs)
            {
                var newGeom = e.Get<PolylineGeometry>().Clone();
                newGeom.Positions = newGeom.Positions.Select(p => resultT * p).ToList();
                cmd.SetTarget(e).SetPolylineGeometry(newGeom);
            }
            cmd.Commit();
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

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    public void Clear()
    {
        _transformBox?.QueueFree();
        _transformBox = null;
        _currTransform = Transform2D.Identity;
        _transformType = -1;
        _processingEs = null;
    }
}