using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class LiquifyInteractor : InteractiveSessionBase
{
    private Entity[] _processingEs;
    private Vector2[][] _origPolylines;
    private Vector2[][] _currPolylines;
    private Rect2[] _aabbs;
    private bool[] _dirty;

    public override void Start(CursorButtonData data)
    {
        _processingEs = LiquifyTargetScope.Resolve(Document, WorkingLayer);
        _origPolylines = new Vector2[_processingEs.Length][];
        _currPolylines = new Vector2[_processingEs.Length][];
        _aabbs = new Rect2[_processingEs.Length];
        _dirty = new bool[_processingEs.Length];

        for (int i = 0; i < _processingEs.Length; i++)
        {
            var positions = _processingEs[i].Get<PolylineGeometry>().Positions.Value;
            _origPolylines[i] = positions.ToArray();
            _currPolylines[i] = positions.ToArray();
            _aabbs[i] = ComputeAabb(_currPolylines[i]);
        }

        ApplyDab(data.WorldPosition, Vector2.Zero, data.Pressure);
    }

    public override void Moving(CursorMotionData data)
    {
        ApplyDab(data.WorldPosition, data.WorldDelta, data.Pressure);
    }

    public override void End(CursorButtonData data)
    {
        var cmd = new CommandBuilder("Liquify", Document);
        for (int i = 0; i < _processingEs.Length; i++)
        {
            if (!_dirty[i]) continue;
            cmd.SetTarget(_processingEs[i]).SetPolylineGeometry(_currPolylines[i].ToImmutableArray());
        }
        cmd.Commit();

        Clear();
    }

    public override void Cancel()
    {
        RestoreViews();
        Clear();
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    private void ApplyDab(Vector2 brushCenter, Vector2 brushDelta, float pressure)
    {
        var liquifyTool = (LiquifyTool)Tool;
        var mode = liquifyTool.Mode.Value;
        var dab = new LiquifyDab(
            brushCenter,
            brushDelta,
            liquifyTool.Radius.Value,
            liquifyTool.Strength.Value,
            pressure);

        // Push moves points by the brush delta, so a polyline whose AABB sits just outside
        // the brush this frame can still be reached next frame. Inflate the cull radius by
        // |delta| to keep that case in scope; Expand/Pinch don't need it but the extra cost
        // is one float add.
        float cullRadius = dab.Radius + brushDelta.Length();

        for (int i = 0; i < _processingEs.Length; i++)
        {
            if (!CircleIntersectsAabb(brushCenter, cullRadius, _aabbs[i]))
                continue;

            var points = _currPolylines[i];
            bool changed = false;
            for (int j = 0; j < points.Length; j++)
            {
                var oldPoint = points[j];
                var newPoint = LiquifySculpt.Apply(mode, oldPoint, dab);
                if (newPoint.IsEqualApprox(oldPoint)) continue;
                points[j] = newPoint;
                changed = true;
            }

            if (changed)
            {
                _dirty[i] = true;
                _aabbs[i] = ComputeAabb(points);
                UpdateView(_processingEs[i], points);
            }
        }
    }

    private void RestoreViews()
    {
        foreach (var (i, e) in _processingEs.Index())
            UpdateView(e, _origPolylines[i]);
    }

    private static void UpdateView(Entity e, IReadOnlyList<Vector2> positions)
    {
        var geom = e.Get<PolylineGeometry>();
        if (e.Has<StrokeSetting>())
            e.Get<StrokeView>().SetGeometry(positions, geom.Radii.Value, geom.Pressures.Value);
        if (e.Has<FilledPolygonSetting>())
            e.Get<Polygon2D>().SetPolygonFromRawRing(positions.ToImmutableArray());
    }

    private void Clear()
    {
        _processingEs = null;
        _origPolylines = null;
        _currPolylines = null;
        _aabbs = null;
        _dirty = null;
    }

    private static Rect2 ComputeAabb(Vector2[] points)
    {
        if (points.Length == 0) return new Rect2();
        var min = points[0];
        var max = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            var p = points[i];
            if (p.X < min.X) min.X = p.X; else if (p.X > max.X) max.X = p.X;
            if (p.Y < min.Y) min.Y = p.Y; else if (p.Y > max.Y) max.Y = p.Y;
        }
        return new Rect2(min, max - min);
    }

    private static bool CircleIntersectsAabb(Vector2 center, float radius, Rect2 aabb)
    {
        float closestX = Mathf.Clamp(center.X, aabb.Position.X, aabb.Position.X + aabb.Size.X);
        float closestY = Mathf.Clamp(center.Y, aabb.Position.Y, aabb.Position.Y + aabb.Size.Y);
        float dx = center.X - closestX;
        float dy = center.Y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }
}
