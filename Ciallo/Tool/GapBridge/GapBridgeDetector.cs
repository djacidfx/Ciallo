using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

internal sealed class GapBridgeDetector
{
    private const int QueryOctagonSides = 8;
    private const float Epsilon = 1e-5f;
    // Business choice: when a loose endpoint is almost as close as the stroke body,
    // users usually expect Gap Bridge to finish the loose end.
    internal const float DanglingEndpointDistancePreference = 1.25f;

    private readonly Arrangement _arr;
    private readonly Dictionary<Entity, GapBridgeCurveInfo> _curves;
    private readonly float _maxGapLength;
    private readonly float _maxDistanceSquared;

    private GapBridgeDetector(Arrangement arr, IReadOnlyCollection<Entity> sourceShapes, float maxGapLength)
    {
        _arr = arr;
        _curves = BuildCurveInfos(arr, sourceShapes);
        _maxGapLength = maxGapLength;
        _maxDistanceSquared = maxGapLength * maxGapLength;
    }

    public static List<GapBridgeTarget> QueryTargets(
        Arrangement arr,
        IReadOnlyCollection<Entity> sourceShapes,
        float maxGapLength)
    {
        var detector = new GapBridgeDetector(arr, sourceShapes, maxGapLength);
        return detector.QueryTargets();
    }

    private List<GapBridgeTarget> QueryTargets()
    {
        var result = new List<GapBridgeTarget>();
        foreach (var (sourceCurve, source) in _curves)
        {
            AddTargetForEndpoint(sourceCurve, source, EndpointSide.Start, result);
            AddTargetForEndpoint(sourceCurve, source, EndpointSide.End, result);
        }
        return result;
    }

    private static Dictionary<Entity, GapBridgeCurveInfo> BuildCurveInfos(
        Arrangement arr,
        IReadOnlyCollection<Entity> sourceShapes)
    {
        var result = new Dictionary<Entity, GapBridgeCurveInfo>();
        foreach (var sourceShape in sourceShapes)
        {
            var positions = GapBridgeGeometry.GetPositions(sourceShape);
            var endpointInfo = arr.GetCurveEndpointInfo(sourceShape.PackedValue);
            result[sourceShape] = new GapBridgeCurveInfo(
                positions,
                IsClosed(positions),
                endpointInfo.StartDangling,
                endpointInfo.EndDangling,
                endpointInfo.StartJunctionLength,
                endpointInfo.EndJunctionLength);
        }
        return result;
    }

    private void AddTargetForEndpoint(
        Entity sourceCurve,
        GapBridgeCurveInfo source,
        EndpointSide sourceSide,
        List<GapBridgeTarget> result)
    {
        var hit = FindBestHit(sourceCurve, source, sourceSide);
        if (hit is not { } best)
            return;

        var sourcePoint = source.EndpointPoint(sourceSide);
        result.Add(new GapBridgeTarget(best.Candidate, [sourcePoint, best.TargetPoint]));
    }

    private CandidateHit? FindBestHit(Entity sourceCurve, GapBridgeCurveInfo source, EndpointSide sourceSide)
    {
        // Source rule: bridges start only from original open-stroke endpoints that are
        // dangling and have at least max-gap visible length before the next junction.
        if (source.IsClosed || !source.EndpointCanStartBridge(sourceSide, _maxGapLength))
            return null;

        var sourcePoint = source.EndpointPoint(sourceSide);
        var sourceT = source.EndpointT(sourceSide);
        CandidateHit? best = TryCreateSameCurveEndpointHit(sourceCurve, source, sourceSide, sourcePoint, sourceT);

        foreach (var (targetCurve, target) in QueryBoundaryCurves(sourcePoint))
        {
            // Same-stroke body hits are ignored; TryCreateSameCurveEndpointHit handles
            // the one allowed same-stroke target: the opposite original endpoint.
            if (targetCurve == sourceCurve)
                continue;

            if (TryCreateBestCurveHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, out var hit))
                KeepBetter(hit, ref best);
        }

        return best;
    }

    private IEnumerable<KeyValuePair<Entity, GapBridgeCurveInfo>> QueryBoundaryCurves(Vector2 sourcePoint)
    {
        // Intentional business tradeoff: this queries curves touched by the octagon boundary.
        // Curves fully contained inside the radius do not count as gap targets for this tool.
        foreach (long targetId in _arr.PolylineQueryCurves(BuildClosedOctagon(sourcePoint, _maxGapLength)))
        {
            var targetCurve = targetId.ToEntity();
            if (_curves.TryGetValue(targetCurve, out var target))
                yield return new KeyValuePair<Entity, GapBridgeCurveInfo>(targetCurve, target);
        }
    }

    private CandidateHit? TryCreateSameCurveEndpointHit(
        Entity sourceCurve,
        GapBridgeCurveInfo source,
        EndpointSide sourceSide,
        Vector2 sourcePoint,
        float sourceT)
    {
        // Same-stroke rule: only the opposite original endpoint may be targeted. It
        // must be dangling, but it does not need to satisfy the source length rule.
        var targetSide = Opposite(sourceSide);
        if (!source.EndpointIsDangling(targetSide))
            return null;

        var targetPoint = source.EndpointPoint(targetSide);
        float distanceSquared = sourcePoint.DistanceSquaredTo(targetPoint);
        if (distanceSquared <= Epsilon * Epsilon || distanceSquared > _maxDistanceSquared)
            return null;

        return new CandidateHit(
            new GapBridgeCandidate(
                sourceCurve,
                sourceT,
                sourceCurve,
                source.EndpointT(targetSide),
                distanceSquared,
                ToTargetKind(targetSide),
                true),
            targetPoint);
    }

    private bool TryCreateBestCurveHit(
        Entity sourceCurve,
        Vector2 sourcePoint,
        float sourceT,
        Entity targetCurve,
        GapBridgeCurveInfo target,
        out CandidateHit hit)
    {
        hit = default;
        CandidateHit? best = null;

        // Cross-stroke targets may be a stroke body or a dangling endpoint within
        // max gap. The source length rule is intentionally not applied to targets,
        // so a short dangling endpoint and a closed stroke body can still be reached.
        var nearestPoint = target.Positions.GetClosestPoint(sourcePoint, out var nearestT);
        var targetKind = ClassifyTargetKind(nearestT, target.LastT);
        KeepBetterIfValid(CreateHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, nearestT, nearestPoint, targetKind), ref best);
        if (TryCreateEndpointHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, EndpointSide.Start, out var startHit))
            KeepBetter(startHit, ref best);
        if (TryCreateEndpointHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, EndpointSide.End, out var endHit))
            KeepBetter(endHit, ref best);

        if (best is not { } found)
            return false;

        hit = found;
        return true;
    }

    private CandidateHit CreateHit(
        Entity sourceCurve,
        Vector2 sourcePoint,
        float sourceT,
        Entity targetCurve,
        GapBridgeCurveInfo target,
        float targetT,
        Vector2 targetPoint,
        GapBridgeTargetKind targetKind)
    {
        return new CandidateHit(
            new GapBridgeCandidate(
                sourceCurve,
                sourceT,
                targetCurve,
                targetT,
                sourcePoint.DistanceSquaredTo(targetPoint),
                targetKind,
                target.EndpointIsDangling(targetKind)),
            targetPoint);
    }

    private bool TryCreateEndpointHit(
        Entity sourceCurve,
        Vector2 sourcePoint,
        float sourceT,
        Entity targetCurve,
        GapBridgeCurveInfo target,
        EndpointSide targetSide,
        out CandidateHit hit)
    {
        hit = default;
        if (!target.EndpointIsDangling(targetSide))
            return false;

        var targetPoint = target.EndpointPoint(targetSide);
        float distanceSquared = sourcePoint.DistanceSquaredTo(targetPoint);
        if (distanceSquared <= Epsilon * Epsilon || distanceSquared > _maxDistanceSquared)
            return false;

        hit = new CandidateHit(
            new GapBridgeCandidate(
                sourceCurve,
                sourceT,
                targetCurve,
                target.EndpointT(targetSide),
                distanceSquared,
                ToTargetKind(targetSide),
                true),
            targetPoint);
        return true;
    }

    private void KeepBetterIfValid(CandidateHit hit, ref CandidateHit? best)
    {
        if (!IsValidGapDistance(hit.Candidate.DistanceSquared))
            return;

        KeepBetter(hit, ref best);
    }

    private bool IsValidGapDistance(float distanceSquared)
    {
        return distanceSquared > Epsilon * Epsilon && distanceSquared <= _maxDistanceSquared;
    }

    private void KeepBetter(CandidateHit hit, ref CandidateHit? best)
    {
        if (best is { } current && !CandidateIsBetter(hit, current))
            return;

        best = hit;
    }

    private bool CandidateIsBetter(CandidateHit candidate, CandidateHit best)
    {
        // Each directional source endpoint keeps one target. Distance leads, with a
        // small preference for loose endpoints over slightly closer stroke bodies.
        float candidateDistance = ComparableDistance(candidate.Candidate);
        float bestDistance = ComparableDistance(best.Candidate);
        if (!Mathf.IsEqualApprox(candidateDistance, bestDistance))
            return candidateDistance < bestDistance;
        if (!Mathf.IsEqualApprox(candidate.Candidate.DistanceSquared, best.Candidate.DistanceSquared))
            return candidate.Candidate.DistanceSquared < best.Candidate.DistanceSquared;
        if (candidate.Candidate.ToCurve.PackedValue != best.Candidate.ToCurve.PackedValue)
            return candidate.Candidate.ToCurve.PackedValue < best.Candidate.ToCurve.PackedValue;
        return candidate.Candidate.ToT < best.Candidate.ToT;
    }

    internal static float ComparableDistance(float distance, bool targetDangling)
    {
        return targetDangling ? distance / DanglingEndpointDistancePreference : distance;
    }

    private static float ComparableDistance(GapBridgeCandidate candidate)
    {
        return ComparableDistance(Mathf.Sqrt(candidate.DistanceSquared), candidate.TargetDangling);
    }

    private static GapBridgeTargetKind ClassifyTargetKind(float t, float lastT)
    {
        if (Mathf.IsEqualApprox(t, 0f))
            return GapBridgeTargetKind.EndpointStart;
        if (Mathf.IsEqualApprox(t, lastT))
            return GapBridgeTargetKind.EndpointEnd;
        return GapBridgeTargetKind.Body;
    }

    private static EndpointSide Opposite(EndpointSide side)
    {
        return side == EndpointSide.Start ? EndpointSide.End : EndpointSide.Start;
    }

    private static GapBridgeTargetKind ToTargetKind(EndpointSide side)
    {
        return side == EndpointSide.Start ? GapBridgeTargetKind.EndpointStart : GapBridgeTargetKind.EndpointEnd;
    }

    private static ImmutableArray<Vector2> BuildClosedOctagon(Vector2 center, float radius)
    {
        var builder = ImmutableArray.CreateBuilder<Vector2>(QueryOctagonSides + 1);
        for (int i = 0; i < QueryOctagonSides; i++)
        {
            float angle = Mathf.Tau * i / QueryOctagonSides;
            builder.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        builder.Add(builder[0]);
        return builder.ToImmutable();
    }

    private static bool IsClosed(ImmutableArray<Vector2> positions)
    {
        return positions.Length >= 2 && positions[0].IsEqualApprox(positions[^1]);
    }

    private enum EndpointSide
    {
        Start,
        End,
    }

    private readonly record struct GapBridgeCurveInfo(
        ImmutableArray<Vector2> Positions,
        bool IsClosed,
        bool StartDangling,
        bool EndDangling,
        float StartJunctionLength,
        float EndJunctionLength)
    {
        public float LastT => Positions.Length - 1f;
        public Vector2 EndpointPoint(EndpointSide side) => side == EndpointSide.Start ? Positions[0] : Positions[^1];
        public float EndpointT(EndpointSide side) => side == EndpointSide.Start ? 0f : LastT;
        public bool EndpointIsDangling(EndpointSide side) => side == EndpointSide.Start ? StartDangling : EndDangling;
        public bool EndpointIsDangling(GapBridgeTargetKind kind)
        {
            return kind switch
            {
                GapBridgeTargetKind.EndpointStart => StartDangling,
                GapBridgeTargetKind.EndpointEnd => EndDangling,
                _ => false,
            };
        }

        public bool EndpointCanStartBridge(EndpointSide side, float minJunctionLength)
        {
            // Deliberately stricter than the old native max-gap/10 rule: only a long
            // dangling endpoint may initiate. A short dangling endpoint can still be
            // reached as the target of a long endpoint, but it never starts the bridge.
            return EndpointIsDangling(side) && EndpointJunctionLength(side) >= minJunctionLength;
        }

        private float EndpointJunctionLength(EndpointSide side)
        {
            return side == EndpointSide.Start ? StartJunctionLength : EndJunctionLength;
        }
    }

    private readonly record struct CandidateHit(
        GapBridgeCandidate Candidate,
        Vector2 TargetPoint);
}
