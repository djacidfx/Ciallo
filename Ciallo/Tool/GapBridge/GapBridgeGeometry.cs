using System.Collections.Generic;
using System.Collections.Immutable;
using Frent;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;
using Godot.Collections;

namespace Ciallo.Tool;

public readonly record struct GapBridgeCandidate(
    Entity FromCurve,
    float FromT,
    Entity ToCurve,
    float ToT,
    float Score);

public readonly record struct GapBridgeTarget(
    GapBridgeCandidate Candidate,
    Vector2 FromPoint,
    Vector2 ToPoint,
    ImmutableArray<Vector2> TargetPolyline);

public static class GapBridgeGeometry
{
    public static List<GapBridgeCandidate> ParseCandidates(Array<Dictionary> raw)
    {
        var result = new List<GapBridgeCandidate>(raw.Count);
        foreach (var dict in raw)
        {
            long fromCurveId = (long)dict["from_curve_id"];
            float fromT = (float)dict["from_t"];
            long toCurveId = (long)dict["to_curve_id"];
            float toT = (float)dict["to_t"];
            float score = (float)dict["score"];
            result.Add(new GapBridgeCandidate(fromCurveId.ToEntity(), fromT, toCurveId.ToEntity(), toT, score));
        }
        return result;
    }

    public static List<GapBridgeTarget> QueryTargets(Arrangement arr, float maxGapLength)
    {
        if (arr == null || maxGapLength <= 0f) return [];

        var candidates = ParseCandidates(arr.GetGapBridgeCandidates(maxGapLength));
        var result = new List<GapBridgeTarget>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (!TryResolveCandidate(candidate, out var fromPoint, out var toPoint))
                continue;

            result.Add(new GapBridgeTarget(candidate, fromPoint, toPoint, [fromPoint, toPoint]));
        }
        return result;
    }

    public static bool TryFindNearestTarget(
        IReadOnlyList<GapBridgeTarget> targets,
        Vector2 worldPosition,
        float hitRadius,
        out GapBridgeTarget target)
    {
        target = default;
        if (targets == null || targets.Count == 0 || hitRadius <= 0f)
            return false;

        float bestDistanceSquared = hitRadius * hitRadius;
        float bestScore = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < targets.Count; i++)
        {
            var candidateTarget = targets[i];
            var polyline = candidateTarget.TargetPolyline;
            if (polyline.Length < 2)
                continue;

            if (!GetBounds(polyline).Grow(hitRadius).HasPoint(worldPosition))
                continue;

            float distanceSquared = DistanceSquaredToPolyline(worldPosition, polyline);
            if (distanceSquared > bestDistanceSquared)
                continue;

            if (!found ||
                distanceSquared < bestDistanceSquared - 1e-4f ||
                (Mathf.IsEqualApprox(distanceSquared, bestDistanceSquared) &&
                 candidateTarget.Candidate.Score > bestScore))
            {
                target = candidateTarget;
                bestDistanceSquared = distanceSquared;
                bestScore = candidateTarget.Candidate.Score;
                found = true;
            }
        }
        return found;
    }

    public static bool TryResolveCandidate(
        GapBridgeCandidate candidate,
        out Vector2 fromPoint,
        out Vector2 toPoint)
    {
        fromPoint = default;
        toPoint = default;
        if (!TryGetPositions(candidate.FromCurve, out var fromPositions) ||
            !TryGetPositions(candidate.ToCurve, out var toPositions))
            return false;

        fromPoint = TrimGeometry.SampleVec2(fromPositions, candidate.FromT);
        toPoint = TrimGeometry.SampleVec2(toPositions, candidate.ToT);
        return !fromPoint.IsEqualApprox(toPoint);
    }

    public static bool TryGetPositions(Entity curve, out ImmutableArray<Vector2> positions)
    {
        positions = [];
        if (!curve.IsAlive || !curve.Has<PolylineGeometry>())
            return false;

        positions = curve.Get<PolylineGeometry>().Positions.Value;
        return positions.Length >= 2;
    }

    public static float SampleFloat(ImmutableArray<float> src, float t, float fallback)
    {
        if (src.Length == 0) return fallback;
        if (src.Length == 1) return src[0];

        t = Mathf.Clamp(t, 0f, src.Length - 1f);
        int i = Mathf.Min((int)Mathf.Floor(t), src.Length - 2);
        float local = t - i;
        return Mathf.Lerp(src[i], src[i + 1], local);
    }

    public static Rect2 GetBounds(IReadOnlyList<Vector2> points)
    {
        var rect = new Rect2(points[0], Vector2.Zero);
        for (int i = 1; i < points.Count; i++)
            rect = rect.Expand(points[i]);
        return rect;
    }

    private static float DistanceSquaredToPolyline(Vector2 point, IReadOnlyList<Vector2> polyline)
    {
        float best = float.PositiveInfinity;
        for (int i = 1; i < polyline.Count; i++)
            best = Mathf.Min(best, DistanceSquaredToSegment(point, polyline[i - 1], polyline[i]));
        return best;
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq <= 1e-6f)
            return point.DistanceSquaredTo(a);

        float t = Mathf.Clamp((point - a).Dot(ab) / lenSq, 0f, 1f);
        return point.DistanceSquaredTo(a + t * ab);
    }
}
