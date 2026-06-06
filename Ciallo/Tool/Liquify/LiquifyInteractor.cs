using System;
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

    public override void Start(CursorButtonData data)
    {
        _processingEs = LiquifyTargetScope.Resolve(Document, WorkingLayer);
        _origPolylines = new Vector2[_processingEs.Length][];
        _currPolylines = new Vector2[_processingEs.Length][];

        for (int i = 0; i < _processingEs.Length; i++)
        {
            var positions = _processingEs[i].Get<PolylineGeometry>().Positions.Value;
            _origPolylines[i] = positions.ToArray();
            _currPolylines[i] = positions.ToArray();
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
        foreach (var (i, e) in _processingEs.Index())
        {
            if (!Changed(i)) continue;
            cmd.SetTarget(e).SetPolylineGeometry(_currPolylines[i].ToImmutableArray());
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

        for (int i = 0; i < _processingEs.Length; i++)
        {
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
                UpdateView(_processingEs[i], points);
        }
    }

    private bool Changed(int polylineIndex)
    {
        for (int i = 0; i < _currPolylines[polylineIndex].Length; i++)
            if (!_currPolylines[polylineIndex][i].IsEqualApprox(_origPolylines[polylineIndex][i]))
                return true;

        return false;
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
    }
}
