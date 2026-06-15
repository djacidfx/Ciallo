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
    Entity Curve,
    Vector2 HitPoint,
    float HitT);

public sealed class PaintStrokeSnapHintManager : IDisposable
{
    private readonly Node2D _root = new();
    private readonly MultiMeshInstance2D _dots;

    public PaintStrokeSnapHintManager(Node2D parent)
    {
        _dots = AutoloadRendering.CreateDots();
        _root.AddChild(_dots);
        parent.AddChild(_root);
        Hide();
    }

    public void Show(IReadOnlyList<Vector2> points)
    {
        _root.Visible = true;
        _dots.SetDotGeometry(points, AppPreference.StrokeDotRadius);
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

        // Single-point strokes are valid snap targets, but Arrangement curve queries cannot return them.
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

        target = new PaintStrokeSnapTarget(curve, hitPoint, t);
        bestDistanceSquared = distanceSquared;
        found = true;
    }

    public static PaintStrokeGeometry BuildRepairedGeometry(
        PolylineGeneratorGeometry geometry,
        PaintStrokeSnapTarget? startTarget,
        PaintStrokeSnapTarget? endTarget,
        float repairLength)
    {
        if (geometry.Count == 0)
            return new PaintStrokeGeometry([], [], [], []);

        var positions = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);
        var radii = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var pressures = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var tilts = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);

        if (geometry.Count == 1)
        {
            var position = startTarget.HasValue
                ? ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true)
                : endTarget.HasValue
                    ? ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false)
                    : geometry.Positions[0];
            positions.Add(position);
            radii.Add(geometry.Radii[0]);
            pressures.Add(geometry.Pressures[0]);
            tilts.Add(geometry.Tilts[0]);
            return new PaintStrokeGeometry(
                positions.ToImmutable(),
                radii.ToImmutable(),
                pressures.ToImmutable(),
                tilts.ToImmutable());
        }

        var arcLengths = new float[geometry.Count];
        for (int i = 1; i < geometry.Count; i++)
            arcLengths[i] = arcLengths[i - 1] + geometry.Positions[i - 1].DistanceTo(geometry.Positions[i]);

        float totalLength = arcLengths[^1];
        var startDelta = startTarget.HasValue
            ? ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true) - geometry.Positions[0]
            : Vector2.Zero;
        var endDelta = endTarget.HasValue
            ? ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false) - geometry.Positions[^1]
            : Vector2.Zero;

        for (int i = 0; i < geometry.Count; i++)
        {
            var position = geometry.Positions[i];
            if (startTarget.HasValue)
            {
                float distanceFromStart = arcLengths[i];
                float startWeight = 1f - Math.Clamp(
                    distanceFromStart / Math.Min(repairLength, Math.Max(totalLength, Epsilon)),
                    0f,
                    1f);
                position += startDelta * startWeight;
            }
            if (endTarget.HasValue)
            {
                float distanceFromEnd = totalLength - arcLengths[i];
                float endWeight = 1f - Math.Clamp(
                    distanceFromEnd / Math.Min(repairLength, Math.Max(totalLength, Epsilon)),
                    0f,
                    1f);
                position += endDelta * endWeight;
            }
            positions.Add(position);
            radii.Add(geometry.Radii[i]);
            pressures.Add(geometry.Pressures[i]);
            tilts.Add(geometry.Tilts[i]);
        }

        return new PaintStrokeGeometry(
            positions.ToImmutable(),
            radii.ToImmutable(),
            pressures.ToImmutable(),
            tilts.ToImmutable());
    }

    private static Vector2 ResolveRepairPoint(
        IReadOnlyList<Vector2> strokePositions,
        PaintStrokeSnapTarget target,
        bool repairStart)
    {
        var targetPositions = target.Curve.Get<PolylineGeometry>().Positions.Value;
        var hitPoint = targetPositions.Sample(target.HitT);
        var t = target.HitT;
        if (t <= Epsilon)
            return targetPositions[0];
        if (t >= targetPositions.Length - 1f - Epsilon)
            return targetPositions[^1];

        var outwardTangent = GetEndpointOutwardTangent(strokePositions, repairStart);
        return hitPoint + outwardTangent * BodyTargetOverrunDistanceWorld;
    }

    private static Vector2 GetEndpointOutwardTangent(IReadOnlyList<Vector2> strokePositions, bool fromStart)
    {
        var endpoint = fromStart ? strokePositions[0] : strokePositions[^1];
        int step = fromStart ? 1 : -1;
        for (int i = fromStart ? 1 : strokePositions.Count - 2;
             i >= 0 && i < strokePositions.Count;
             i += step)
        {
            var tangent = endpoint - strokePositions[i];
            if (tangent.LengthSquared() > Epsilon * Epsilon)
                return tangent.Normalized();
        }

        return Vector2.Zero;
    }

}

public readonly record struct PaintStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);
