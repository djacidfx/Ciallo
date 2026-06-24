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
    private readonly HashSet<Entity> _seen = [];

    public PaintStrokeSnapTarget? TryFindTarget(
        Arrangement arr,
        Vector2 worldPosition,
        float snapDistance)
    {
        if (snapDistance <= 0f)
            return null;

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

        if (foundEndpoint)
            return endpointTarget;
        if (foundBody)
            return bodyTarget;
        return null;

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

        // Snapped endpoint(s) become HARD constraints; the free interior vertices are deformed to
        // preserve the stroke's shape (turning-angle gesture) so the span rotates to absorb the endpoint
        // move instead of shearing. The pierce point already sits on the body-bulk's opposite side,
        // so the endpoint->neighbour segment crosses the target; shape preservation keeps that
        // neighbour near where it was drawn, so the crossing is retained.
        var fixedPositions = new Dictionary<int, Vector2>(2);
        if (startTarget.HasValue)
            fixedPositions[0] = ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true);
        if (endTarget.HasValue)
            fixedPositions[geometry.Count - 1] =
                ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false);

        var deformed = PolylineShapeOptimizer.PreserveShape(
            geometry.Positions,
            fixedPositions);

        return CreateGeometry(geometry, deformed);
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

        int segIndex = Mathf.Min((int)t, targetPositions.Length - 2);
        var segDir = targetPositions[segIndex + 1] - targetPositions[segIndex];
        var normal = new Vector2(segDir.Y, -segDir.X).Normalized();

        // Overrun straight along the target-segment NORMAL. The repaired endpoint must land on the
        // side OPPOSITE the stroke's body bulk (forces the crossing when not pierced; keeps the
        // endpoint on the tail side it already pierced to when pierced).
        //
        // sd is measured against ONE target segment's INFINITE line, so the body-side scan must stay
        // LOCAL to the hit: a non-pierced but curved stroke can place a distant point on the far side
        // of the extended line without ever crossing the real target curve. Treating that as a flip
        // would wrongly push the endpoint back onto the body side (no crossing -> snap fails). So the
        // default body side is the immediate off-line neighbour; a flip only counts as a real pierce
        // when it happens within a local radius of the endpoint (a snap overshoot is small).
        int endpoint = repairStart ? 0 : strokePositions.Count - 1;
        int step = repairStart ? 1 : -1;
        float tailSd = 0f;
        float bodyBulkSd = 0f;
        float localRadius = 0f;
        for (int i = endpoint + step; i >= 0 && i < strokePositions.Count; i += step)
        {
            var p = strokePositions[i];
            float sd = (p - hitPoint).Dot(normal);
            if (Mathf.Abs(sd) <= Epsilon)
                continue;
            if (tailSd == 0f)
            {
                tailSd = sd;
                bodyBulkSd = sd; // default: not pierced -> body bulk == tail side (local neighbour)
                // Local window = a few times the endpoint->neighbour spacing; a genuine snap pierce
                // overshoots by only ~BodyTargetOverrunDistanceWorld, so the body-side flip sits close.
                localRadius = 4f * (p - hitPoint).Length();
                continue;
            }
            if ((p - hitPoint).Length() > localRadius)
                break; // left the local crossing region; anything beyond is unrelated curvature
            if (Mathf.Sign(sd) != Mathf.Sign(tailSd))
            {
                bodyBulkSd = sd; // local flip back toward the body bulk == real pierce
                break;
            }
        }
        float bodyBulkSide = bodyBulkSd >= 0f ? 1f : -1f;
        return hitPoint - bodyBulkSide * normal * BodyTargetOverrunDistanceWorld;
    }
}

public readonly record struct PaintStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);
