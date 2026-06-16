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
    private const float EndpointPreferenceSnapDistanceRatio = 1f / 3;
    // Laplacian smoothness weight is implicitly 1.0; DisplacementWeight is relative to it.
    private const double DisplacementWeight = 0.08;
    private const double FarDisplacementPenalty = 24.0;

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

        float snapDistanceSquared = snapDistance * snapDistance;
        float endpointPreferenceDistance = snapDistance * EndpointPreferenceSnapDistanceRatio;
        float endpointPreferenceDistanceSquared = endpointPreferenceDistance * endpointPreferenceDistance;
        var endpointTarget = default(PaintStrokeSnapTarget);
        var bodyTarget = default(PaintStrokeSnapTarget);
        float bestEndpointDistanceSquared = endpointPreferenceDistanceSquared;
        float bestBodyDistanceSquared = snapDistanceSquared;
        bool foundEndpoint = false;
        bool foundBody = false;
        var seen = new HashSet<Entity>();

        foreach (long targetId in arr.PolylineQueryCurves(PolylineShapeBuilder.BuildClosedOctagon(worldPosition, snapDistance)))
        {
            var curve = targetId.ToEntity();
            if (sourceShapes.Contains(curve) && seen.Add(curve))
                KeepIfNearer(curve);
        }

        // Single-point strokes are valid snap targets, but Arrangement curve queries cannot return them.
        foreach (var curve in sourceShapes)
            if (seen.Add(curve) && curve.Get<PolylineGeometry>().Positions.Value.GetBoundingBox().Grow(snapDistance).HasPoint(worldPosition))
                KeepIfNearer(curve);

        target = foundEndpoint ? endpointTarget : bodyTarget;
        return foundEndpoint || foundBody;

        void KeepIfNearer(Entity curve)
        {
            var positions = curve.Get<PolylineGeometry>().Positions.Value;

            // Prefer curve endpoints only in the tighter endpoint-preference range.
            // Body targets can still use the full snap distance.
            float startDistSq = worldPosition.DistanceSquaredTo(positions[0]);
            float endDistSq = worldPosition.DistanceSquaredTo(positions[^1]);
            bool startInRange = startDistSq <= endpointPreferenceDistanceSquared;
            bool endInRange = endDistSq <= endpointPreferenceDistanceSquared;

            if (startInRange || endInRange)
            {
                bool useStart = startInRange && (!endInRange || startDistSq <= endDistSq);
                var endpointPoint = useStart ? positions[0] : positions[^1];
                float endpointT = useStart ? 0f : positions.Length - 1f;
                float endpointDistSq = useStart ? startDistSq : endDistSq;

                if (!foundEndpoint || endpointDistSq <= bestEndpointDistanceSquared)
                {
                    endpointTarget = new PaintStrokeSnapTarget(curve, endpointPoint, endpointT);
                    bestEndpointDistanceSquared = endpointDistSq;
                    foundEndpoint = true;
                }
                return;
            }

            var hitPoint = positions.GetClosestPoint(worldPosition, out var t);
            float distanceSquared = worldPosition.DistanceSquaredTo(hitPoint);
            if (distanceSquared > bestBodyDistanceSquared)
                return;

            bodyTarget = new PaintStrokeSnapTarget(curve, hitPoint, t);
            bestBodyDistanceSquared = distanceSquared;
            foundBody = true;
        }
    }

    public static PaintStrokeGeometry BuildRepairedGeometry(
        PolylineGeneratorGeometry geometry,
        PaintStrokeSnapTarget? startTarget,
        PaintStrokeSnapTarget? endTarget)
    {
        if (geometry.Count == 0)
            return new PaintStrokeGeometry([], [], [], []);

        if (geometry.Count == 1)
        {
            var position = startTarget.HasValue
                ? ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true)
                : endTarget.HasValue
                    ? ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false)
                    : geometry.Positions[0];
            return CreateGeometry(geometry, [position]);
        }

        if (!startTarget.HasValue && !endTarget.HasValue)
            return CreateGeometry(geometry, geometry.Positions);

        // Paint Stroke Snap is not smoothing the drawn stroke itself. The user already
        // authored that curve; snap repair only distributes endpoint correction across
        // the new stroke without creating the hard kink produced by the old local ramp.
        //
        // Business requirement:
        // - snapped endpoints must land exactly on the repair point;
        // - nearby points may follow the endpoint so the connection looks intentional;
        // - distant points should move less, preserving the user's stroke away from the snap;
        // - "Snap distance" is only the hit-test radius, not a repair-range cutoff.
        //
        // The snapped endpoints are both the hard constraints and the penalty origins, so the
        // displacement-Laplacian penalty grows with distance from them. See
        // PolylineExtension.SolveDisplacementLaplacian for the full model.
        var fixedDisplacements = new Dictionary<int, Vector2>(2);
        var penaltyOrigins = new List<int>(2);
        if (startTarget.HasValue)
        {
            fixedDisplacements[0] = ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true) - geometry.Positions[0];
            penaltyOrigins.Add(0);
        }
        if (endTarget.HasValue)
        {
            int last = geometry.Count - 1;
            fixedDisplacements[last] = ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false) - geometry.Positions[last];
            penaltyOrigins.Add(last);
        }

        var repairedPositions = PolylineExtension.SolveDisplacementLaplacian(
            geometry.Positions,
            fixedDisplacements,
            penaltyOrigins,
            DisplacementWeight,
            FarDisplacementPenalty);

        return CreateGeometry(geometry, repairedPositions);
    }

    private static PaintStrokeGeometry CreateGeometry(
        PolylineGeneratorGeometry geometry,
        IReadOnlyList<Vector2> positions)
    {
        var repairedPositions = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);
        var radii = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var pressures = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var tilts = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);

        for (int i = 0; i < geometry.Count; i++)
        {
            repairedPositions.Add(positions[i]);
            radii.Add(geometry.Radii[i]);
            pressures.Add(geometry.Pressures[i]);
            tilts.Add(geometry.Tilts[i]);
        }

        return new PaintStrokeGeometry(
            repairedPositions.ToImmutable(),
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
