using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

internal sealed class GapBridgeDetector
{
    private const float Epsilon = 1e-5f;

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

    public static List<GapBridge> QueryBridges(
        Arrangement arr,
        IReadOnlyCollection<Entity> sourceShapes,
        float maxGapLength)
    {
        var detector = new GapBridgeDetector(arr, sourceShapes, maxGapLength);
        return detector.QueryBridges();
    }

    private List<GapBridge> QueryBridges()
    {
        var result = new List<GapBridge>();
        foreach (var (sourceCurve, source) in _curves)
        {
            AddBridgeForEndpoint(sourceCurve, source, EndpointSide.Start, result);
            AddBridgeForEndpoint(sourceCurve, source, EndpointSide.End, result);
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
            var positions = sourceShape.Get<PolylineGeometry>().Positions.Value;
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

    private void AddBridgeForEndpoint(
        Entity sourceCurve,
        GapBridgeCurveInfo source,
        EndpointSide sourceSide,
        List<GapBridge> result)
    {
        var hit = FindBestHit(sourceCurve, source, sourceSide);
        if (hit is not { } best)
            return;

        result.Add(new GapBridge(
            sourceCurve,
            sourceSide == EndpointSide.Start,
            source.EndpointPoint(sourceSide),
            best.TargetPoint,
            best.Candidate.TargetKind == GapBridgeTargetKind.Body,
            best.Candidate.DistanceSquared));
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
        foreach (long targetId in _arr.PolylineQueryCurves(PolylineShapeBuilder.BuildClosedOctagon(sourcePoint, _maxGapLength)))
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
                ToTargetKind(targetSide)),
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
        if (targetKind == GapBridgeTargetKind.Body)
        {
            // A dangling endpoint owns the terminal gap-length of its curve. If the
            // nearest body point is still in that tail, the artist most likely meant
            // endpoint-to-endpoint closure rather than an overpassing body bridge.
            bool startOwnsHit = target.EndpointIsDangling(EndpointSide.Start)
                && target.Positions.GetLength(0f, nearestT) <= _maxGapLength;
            bool endOwnsHit = target.EndpointIsDangling(EndpointSide.End)
                && target.Positions.GetLength(nearestT, target.LastT) <= _maxGapLength;
            float startDistanceSquared = sourcePoint.DistanceSquaredTo(target.EndpointPoint(EndpointSide.Start));
            float endDistanceSquared = sourcePoint.DistanceSquaredTo(target.EndpointPoint(EndpointSide.End));
            startOwnsHit = startOwnsHit && IsValidGapDistance(startDistanceSquared);
            endOwnsHit = endOwnsHit && IsValidGapDistance(endDistanceSquared);

            if (startOwnsHit && endOwnsHit)
            {
                targetKind = startDistanceSquared <= endDistanceSquared
                    ? GapBridgeTargetKind.EndpointStart
                    : GapBridgeTargetKind.EndpointEnd;
            }
            else if (startOwnsHit)
            {
                targetKind = GapBridgeTargetKind.EndpointStart;
            }
            else if (endOwnsHit)
            {
                targetKind = GapBridgeTargetKind.EndpointEnd;
            }
        }

        KeepBetterIfValid(CreateHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, nearestT, nearestPoint, targetKind), ref best);
        if (targetKind != GapBridgeTargetKind.EndpointStart
            && TryCreateEndpointHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, EndpointSide.Start, out var startHit))
            KeepBetter(startHit, ref best);
        if (targetKind != GapBridgeTargetKind.EndpointEnd
            && TryCreateEndpointHit(sourceCurve, sourcePoint, sourceT, targetCurve, target, EndpointSide.End, out var endHit))
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
        // Endpoint targets must repair to the exact original endpoint. A nearest
        // body query can classify a nearly-endpoint hit as an endpoint, but keeping
        // its sampled t/point would leave tiny gaps that exact arrangement geometry
        // still treats as open.
        switch (targetKind)
        {
            case GapBridgeTargetKind.EndpointStart:
                targetT = 0f;
                targetPoint = target.EndpointPoint(EndpointSide.Start);
                break;
            case GapBridgeTargetKind.EndpointEnd:
                targetT = target.LastT;
                targetPoint = target.EndpointPoint(EndpointSide.End);
                break;
        }

        return new CandidateHit(
            new GapBridgeCandidate(
                sourceCurve,
                sourceT,
                targetCurve,
                targetT,
                sourcePoint.DistanceSquaredTo(targetPoint),
                targetKind),
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
                ToTargetKind(targetSide)),
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
        // Each directional source endpoint keeps one target. After target kind is
        // resolved, ranking uses the real gap distance.
        if (!Mathf.IsEqualApprox(candidate.Candidate.DistanceSquared, best.Candidate.DistanceSquared))
            return candidate.Candidate.DistanceSquared < best.Candidate.DistanceSquared;
        if (candidate.Candidate.ToCurve.PackedValue != best.Candidate.ToCurve.PackedValue)
            return candidate.Candidate.ToCurve.PackedValue < best.Candidate.ToCurve.PackedValue;
        return candidate.Candidate.ToT < best.Candidate.ToT;
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

    private static bool IsClosed(ImmutableArray<Vector2> positions)
    {
        return positions.Length >= 2 && positions[0].IsEqualApprox(positions[^1]);
    }

    private enum EndpointSide
    {
        Start,
        End,
    }

    // Internal target classification: only used while the detector decides which exact
    // point to land on. Consumers see the resolved point plus GapBridge.TargetIsBody.
    private enum GapBridgeTargetKind
    {
        EndpointStart,
        EndpointEnd,
        Body,
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

    // Internal: pairs a candidate with its resolved landing point during ranking.
    private readonly record struct CandidateHit(
        GapBridgeCandidate Candidate,
        Vector2 TargetPoint);

    // Internal ranking record. Carries t-space bookkeeping (FromT/ToCurve/ToT) used only
    // to break ranking ties deterministically; the public GapBridge keeps just the result.
    private readonly record struct GapBridgeCandidate(
        Entity FromCurve,
        float FromT,
        Entity ToCurve,
        float ToT,
        float DistanceSquared,
        GapBridgeTargetKind TargetKind);
}
