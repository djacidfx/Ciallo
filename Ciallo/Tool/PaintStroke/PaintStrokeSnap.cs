using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

public readonly record struct PaintStrokeSnapTarget(
    Entity Curve,
    Vector2 HitPoint,
    float HitT);

public sealed class PaintStrokeSnap
{
    private const float Epsilon = 1e-5f;
    private const float BodyTargetOverrunDistanceWorld = 0.05f;
    private const float EndpointPreferenceSnapDistanceRatio = 1f / 3;
    // Laplacian smoothness weight is implicitly 1.0; these values control the baseline penalty and distance falloff penalty scale.
    private const double DisplacementPenaltyWeight = 0.08;
    private const double FalloffPenaltyScale = 4.0;

    private readonly HashSet<Entity> _seen = [];

    public bool TryFindTarget(
        Arrangement arr,
        Vector2 worldPosition,
        float snapDistance,
        out PaintStrokeSnapTarget target)
    {
        target = default;
        if (snapDistance <= 0f)
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
        _seen.Clear();

        // Although single-point strokes are theoretically valid snap targets, but Arrangement curve queries cannot return them, so we discard them intentionally.
        foreach (long targetId in arr.PolylineQueryCurves(PolylineShapeBuilder.BuildClosedOctagon(worldPosition, snapDistance)))
        {
            var curve = targetId.ToEntity();
            if (_seen.Add(curve))
                KeepIfNearer(curve);
        }

        target = foundEndpoint ? endpointTarget : bodyTarget;
        return foundEndpoint || foundBody;

        void KeepIfNearer(Entity curve)
        {
            var positions = curve.Get<SampledPolyline>().Positions.Value;

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
            DisplacementPenaltyWeight,
            FalloffPenaltyScale);

        return CreateGeometry(geometry, repairedPositions);
    }

    private static PaintStrokeGeometry CreateGeometry(
        PolylineGeneratorGeometry geometry,
        IReadOnlyList<Vector2> positions)
    {
        var repairedPositions = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);

        for (int i = 0; i < geometry.Count; i++)
        {
            repairedPositions.Add(positions[i]);
        }

        return new PaintStrokeGeometry(
            repairedPositions.MoveToImmutable(),
            geometry.Radii.ToImmutableArray(),
            geometry.Pressures.ToImmutableArray(),
            geometry.Tilts.ToImmutableArray());
    }

    private static Vector2 ResolveRepairPoint(
        IReadOnlyList<Vector2> strokePositions,
        PaintStrokeSnapTarget target,
        bool repairStart)
    {
        var targetPositions = target.Curve.Get<SampledPolyline>().Positions.Value;
        var hitPoint = targetPositions.Sample(target.HitT);
        var t = target.HitT;
        // Endpoint snap targets land exactly on the endpoint; no overrun needed.
        if (t <= Epsilon)
            return targetPositions[0];
        if (t >= targetPositions.Length - 1f - Epsilon)
            return targetPositions[^1];

        // ponytail: overrun along target-segment normal guarantees piercing topologically.
        int segIndex = Mathf.Min((int)t, targetPositions.Length - 2);
        var segDir = targetPositions[segIndex + 1] - targetPositions[segIndex];
        var normal = new Vector2(segDir.Y, -segDir.X).Normalized();

        // Walk inward to find a point far enough from hitPoint for a stable direction.
        var toInner = Vector2.Zero;
        if (strokePositions.Count > 1)
        {
            int start = repairStart ? 1 : strokePositions.Count - 2;
            int step = repairStart ? 1 : -1;
            for (int i = start; i >= 0 && i < strokePositions.Count; i += step)
            {
                var candidate = hitPoint - strokePositions[i];
                if (candidate.LengthSquared() > Epsilon * Epsilon)
                {
                    toInner = candidate;
                    break;
                }
            }
        }
        // ponytail: if all points coincide with hitPoint, either direction pierces.
        if (toInner.Dot(normal) < 0)
            normal = -normal;

        return hitPoint + normal * BodyTargetOverrunDistanceWorld;
    }
}

public readonly record struct PaintStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);
