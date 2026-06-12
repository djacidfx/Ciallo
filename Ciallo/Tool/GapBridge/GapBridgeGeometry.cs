using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

public enum GapBridgeTargetKind
{
    EndpointStart,
    EndpointEnd,
    Body,
}

public readonly record struct GapBridgeCandidate(
    Entity FromCurve,
    float FromT,
    Entity ToCurve,
    float ToT,
    float DistanceSquared,
    GapBridgeTargetKind TargetKind,
    bool TargetDangling);

public readonly record struct GapBridgeTarget(
    GapBridgeCandidate Candidate,
    ImmutableArray<Vector2> TargetPolyline);

public static class GapBridgeGeometry
{
    public static List<GapBridgeTarget> QueryTargets(Arrangement arr, IReadOnlyCollection<Entity> sourceShapes, float maxGapLength)
    {
        return GapBridgeDetector.QueryTargets(arr, sourceShapes, maxGapLength);
    }

    public static bool TryFindNearestTarget(
        IReadOnlyList<GapBridgeTarget> targets,
        Vector2 worldPosition,
        float hitRadius,
        out GapBridgeTarget target)
    {
        target = default;
        if (targets.Count == 0 || hitRadius <= 0f)
            return false;

        float bestDistanceSquared = hitRadius * hitRadius;
        float bestCandidateDistanceSquared = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < targets.Count; i++)
        {
            var candidateTarget = targets[i];
            var polyline = candidateTarget.TargetPolyline;

            if (!polyline.GetBoundingBox().Grow(hitRadius).HasPoint(worldPosition))
                continue;

            var closestPoint = polyline.GetClosestPoint(worldPosition, out _);
            float distanceSquared = worldPosition.DistanceSquaredTo(closestPoint);
            if (distanceSquared > bestDistanceSquared)
                continue;

            float candidateDistanceSquared = candidateTarget.Candidate.DistanceSquared;
            if (!found ||
                distanceSquared < bestDistanceSquared - 1e-4f ||
                (Mathf.IsEqualApprox(distanceSquared, bestDistanceSquared) &&
                 candidateDistanceSquared < bestCandidateDistanceSquared))
            {
                target = candidateTarget;
                bestDistanceSquared = distanceSquared;
                bestCandidateDistanceSquared = candidateDistanceSquared;
                found = true;
            }
        }
        return found;
    }

    public static (Vector2 FromPoint, Vector2 ToPoint) ResolveCandidate(GapBridgeCandidate candidate)
    {
        var fromPositions = GetPositions(candidate.FromCurve);
        var toPositions = GetPositions(candidate.ToCurve);
        return (
            fromPositions.Sample(candidate.FromT),
            toPositions.Sample(candidate.ToT));
    }

    public static ImmutableArray<Vector2> GetPositions(Entity curve)
    {
        return curve.Get<PolylineGeometry>().Positions.Value;
    }
}
