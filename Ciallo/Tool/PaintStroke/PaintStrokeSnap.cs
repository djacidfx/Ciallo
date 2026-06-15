using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public readonly record struct PaintStrokeSnapTarget(
    Vector2 HitPoint,
    Vector2 RepairPoint,
    float DistanceSquared);

public sealed class PaintStrokeSnapPreviewManager : IDisposable
{
    private readonly Node2D _root = new();
    private readonly StrokeView _dash;

    public PaintStrokeSnapPreviewManager(Node2D parent)
    {
        _dash = new StrokeView { Material = AutoloadRendering.DashWireframeMaterial };
        _root.AddChild(_dash);
        parent.AddChild(_root);
        Hide();
    }

    public void Show(Vector2 dashFrom, Vector2 dashTo)
    {
        _root.Visible = true;
        _dash.Visible = true;
        _dash.SetGeometry([dashFrom, dashTo], AppPreference.StrokeWireframeRadius * 2f);
    }

    public void Hide()
    {
        _root.Visible = false;
    }

    public void Dispose()
    {
        _root.QueueFree();
    }

}

public static class PaintStrokeSnap
{
    private const float Epsilon = 1e-5f;
    private const float BodyTargetOverrunDistanceWorld = 0.1f;

    public static bool TryFindTarget(
        Arrangement arr,
        IReadOnlySet<Entity> sourceShapes,
        Vector2 worldPosition,
        float snapDistance,
        out PaintStrokeSnapTarget target)
    {
        target = default;
        if (arr == null || snapDistance <= 0f)
            return false;

        float bestDistanceSquared = snapDistance * snapDistance;
        bool found = false;
        var seen = new HashSet<Entity>();

        foreach (long targetId in arr.PolylineQueryCurves(PolylineShapeBuilder.BuildClosedOctagon(worldPosition, snapDistance)))
        {
            var curve = targetId.ToEntity();
            if (sourceShapes.Contains(curve) && seen.Add(curve))
                KeepIfNearer(curve, worldPosition, ref target, ref bestDistanceSquared, ref found);
        }

        foreach (var curve in sourceShapes)
            if (seen.Add(curve) && curve.Get<PolylineGeometry>().Positions.Value.GetBoundingBox().Grow(snapDistance).HasPoint(worldPosition))
                KeepIfNearer(curve, worldPosition, ref target, ref bestDistanceSquared, ref found);

        return found;
    }

    private static void KeepIfNearer(
        Entity curve,
        Vector2 worldPosition,
        ref PaintStrokeSnapTarget target,
        ref float bestDistanceSquared,
        ref bool found)
    {
        var positions = curve.Get<PolylineGeometry>().Positions.Value;
        var hitPoint = positions.GetClosestPoint(worldPosition, out var t);
        float distanceSquared = worldPosition.DistanceSquaredTo(hitPoint);
        if (distanceSquared > bestDistanceSquared)
            return;

        var repairPoint = ResolveRepairPoint(positions, t, hitPoint, worldPosition);
        target = new PaintStrokeSnapTarget(hitPoint, repairPoint, distanceSquared);
        bestDistanceSquared = distanceSquared;
        found = true;
    }

    public static PaintStrokeGeometry BuildGeometry(
        PolylineGeneratorGeometry geometry,
        PaintStrokeSnapTarget? startTarget,
        PaintStrokeSnapTarget? endTarget)
    {
        var positions = new List<Vector2>(geometry.Count + 2);
        var radii = new List<float>(geometry.Count + 2);
        var pressures = new List<float>(geometry.Count + 2);
        var tilts = new List<Vector2>(geometry.Count + 2);

        FillGeometry(geometry, startTarget, endTarget, positions, radii, pressures, tilts);

        return new PaintStrokeGeometry(
            positions.ToImmutableArray(),
            radii.ToImmutableArray(),
            pressures.ToImmutableArray(),
            tilts.ToImmutableArray());
    }

    public static void FillGeometry(
        PolylineGeneratorGeometry geometry,
        PaintStrokeSnapTarget? startTarget,
        PaintStrokeSnapTarget? endTarget,
        List<Vector2> positions,
        List<float> radii,
        List<float> pressures,
        List<Vector2> tilts)
    {
        positions.Clear();
        radii.Clear();
        pressures.Clear();
        tilts.Clear();

        if (geometry.Count == 0)
            return;

        if (startTarget is { } start)
            AddSample(start.RepairPoint, geometry.Radii[0], geometry.Pressures[0], geometry.Tilts[0]);

        for (int i = 0; i < geometry.Count; i++)
            AddSample(geometry.Positions[i], geometry.Radii[i], geometry.Pressures[i], geometry.Tilts[i]);

        if (endTarget is { } end)
            AddSample(end.RepairPoint, geometry.Radii[^1], geometry.Pressures[^1], geometry.Tilts[^1]);

        void AddSample(Vector2 position, float radius, float pressure, Vector2 tilt)
        {
            if (positions.Count > 0 && positions[^1].IsEqualApprox(position))
                return;

            positions.Add(position);
            radii.Add(radius);
            pressures.Add(pressure);
            tilts.Add(tilt);
        }
    }

    public static bool TryResolveStartDirection(IReadOnlyList<Vector2> positions, PaintStrokeSnapTarget target, out bool allowed)
    {
        allowed = false;
        if (positions.Count < 2)
            return false;

        var snapDirection = positions[0] - target.HitPoint;
        if (snapDirection.LengthSquared() <= Epsilon * Epsilon)
            return true;

        for (int i = 1; i < positions.Count; i++)
        {
            var travelDirection = positions[i] - positions[0];
            // Pen pressure changes can also arrive as cursor move events, producing duplicate positions before spatial travel exists.
            if (travelDirection.LengthSquared() <= Epsilon * Epsilon)
                continue;

            allowed = travelDirection.Dot(snapDirection) >= 0f;
            return true;
        }

        return false;
    }

    public static bool EndDirectionAllowsSnap(IReadOnlyList<Vector2> positions, PaintStrokeSnapTarget target)
    {
        if (positions.Count < 2)
            return false;

        var snapDirection = target.HitPoint - positions[^1];
        if (snapDirection.LengthSquared() <= Epsilon * Epsilon)
            return false;

        for (int i = positions.Count - 2; i >= 0; i--)
        {
            var travelDirection = positions[^1] - positions[i];
            if (travelDirection.LengthSquared() > Epsilon * Epsilon)
                return travelDirection.Dot(snapDirection) >= 0f;
        }

        return false;
    }

    private static Vector2 ResolveRepairPoint(
        ImmutableArray<Vector2> positions,
        float t,
        Vector2 hitPoint,
        Vector2 worldPosition)
    {
        if (Mathf.IsEqualApprox(t, 0f))
            return positions[0];
        if (Mathf.IsEqualApprox(t, positions.Length - 1f))
            return positions[^1];
        if (Mathf.IsEqualApprox(t, MathF.Round(t)))
            return positions[(int)MathF.Round(t)];

        var direction = (hitPoint - worldPosition).Normalized();
        return hitPoint + direction * BodyTargetOverrunDistanceWorld;
    }

}

public readonly record struct PaintStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);
