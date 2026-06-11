using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.Tool;

public readonly record struct GapBridgeStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);

public static class GapBridgeRepairGeometry
{
    private const int SmoothBridgeSegments = 12;
    private const float Epsilon = 1e-5f;

    public static ImmutableArray<Vector2> BuildPolyline(
        GapBridgeCandidate candidate,
        Vector2 fromPoint,
        Vector2 toPoint)
    {
        if (!GapBridgeGeometry.TryGetPositions(candidate.FromCurve, out var fromPositions) ||
            !GapBridgeGeometry.TryGetPositions(candidate.ToCurve, out var toPositions))
            return CleanPolyline(fromPoint, toPoint);

        if (!TryChooseTangents(
                fromPositions,
                candidate.FromT,
                toPositions,
                candidate.ToT,
                fromPoint,
                toPoint,
                out var fromTangent,
                out var toTangent))
            return CleanPolyline(fromPoint, toPoint);

        if (TryBuildSharpCorner(fromPoint, fromTangent, toPoint, toTangent, out var corner))
            return CleanPolyline(fromPoint, corner, toPoint);

        return BuildSmoothBridge(fromPoint, fromTangent, toPoint, toTangent);
    }

    public static GapBridgeStrokeGeometry BuildStrokeGeometry(GapBridgeTarget target)
    {
        var positions = target.TargetPolyline;
        if (positions.Length < 2)
            return new GapBridgeStrokeGeometry([], [], [], []);

        var candidate = target.Candidate;
        float fromRadius = SampleRadius(candidate.FromCurve, candidate.FromT);
        float toRadius = SampleRadius(candidate.ToCurve, candidate.ToT);
        float fromPressure = SamplePressure(candidate.FromCurve, candidate.FromT);
        float toPressure = SamplePressure(candidate.ToCurve, candidate.ToT);
        var fromTilt = SampleTilt(candidate.FromCurve, candidate.FromT);
        var toTilt = SampleTilt(candidate.ToCurve, candidate.ToT);

        var radii = ImmutableArray.CreateBuilder<float>(positions.Length);
        var pressures = ImmutableArray.CreateBuilder<float>(positions.Length);
        var tilts = ImmutableArray.CreateBuilder<Vector2>(positions.Length);
        for (int i = 0; i < positions.Length; i++)
        {
            float u = positions.Length == 1 ? 0f : (float)i / (positions.Length - 1);
            radii.Add(Mathf.Max(0.1f, Mathf.Lerp(fromRadius, toRadius, u)));
            pressures.Add(Mathf.Lerp(fromPressure, toPressure, u));
            tilts.Add(fromTilt.Lerp(toTilt, u));
        }

        return new GapBridgeStrokeGeometry(
            positions,
            radii.ToImmutable(),
            pressures.ToImmutable(),
            tilts.ToImmutable());
    }

    private static bool TryChooseTangents(
        ImmutableArray<Vector2> fromPositions,
        float fromT,
        ImmutableArray<Vector2> toPositions,
        float toT,
        Vector2 fromPoint,
        Vector2 toPoint,
        out Vector2 fromTangent,
        out Vector2 toTangent)
    {
        fromTangent = default;
        toTangent = default;

        var fromTo = NormalizeOrZero(toPoint - fromPoint);
        if (fromTo == Vector2.Zero)
            return false;

        var fromCandidates = GetCompletionTangents(fromPositions, fromT, fromTo);
        var toCandidates = GetCompletionTangents(toPositions, toT, -fromTo);
        if (fromCandidates.Count == 0 || toCandidates.Count == 0)
            return false;

        float bestScore = float.NegativeInfinity;
        foreach (var fromCandidate in fromCandidates)
        {
            foreach (var toCandidate in toCandidates)
            {
                float score = fromCandidate.Dot(fromTo) + toCandidate.Dot(-fromTo);
                if (score <= bestScore)
                    continue;

                fromTangent = fromCandidate;
                toTangent = toCandidate;
                bestScore = score;
            }
        }
        return true;
    }

    private static List<Vector2> GetCompletionTangents(
        ImmutableArray<Vector2> positions,
        float t,
        Vector2 toward)
    {
        var result = new List<Vector2>(2);
        if (positions.Length < 2)
            return result;

        float maxT = positions.Length - 1f;
        if (t <= 1e-3f)
        {
            AddUniqueNormalized(result, positions[0] - positions[1]);
            return result;
        }

        if (t >= maxT - 1e-3f)
        {
            AddUniqueNormalized(result, positions[^1] - positions[^2]);
            return result;
        }

        int seg = Math.Clamp((int)MathF.Floor(t), 0, positions.Length - 2);
        AddUniqueNormalized(result, positions[seg + 1] - positions[seg]);
        AddUniqueNormalized(result, positions[seg] - positions[seg + 1]);

        result.Sort((a, b) => b.Dot(toward).CompareTo(a.Dot(toward)));
        return result;
    }

    private static bool TryBuildSharpCorner(
        Vector2 fromPoint,
        Vector2 fromTangent,
        Vector2 toPoint,
        Vector2 toTangent,
        out Vector2 corner)
    {
        corner = default;
        float gapLength = fromPoint.DistanceTo(toPoint);
        if (gapLength <= Epsilon)
            return false;

        float denom = Cross(fromTangent, toTangent);
        if (Mathf.Abs(denom) < 0.25f)
            return false;

        var delta = toPoint - fromPoint;
        float fromDistance = Cross(delta, toTangent) / denom;
        float toDistance = Cross(delta, fromTangent) / denom;
        if (fromDistance <= Epsilon || toDistance <= Epsilon)
            return false;

        corner = fromPoint + fromTangent * fromDistance;
        float maxCornerOffset = gapLength * 2f;
        if (DistanceSquaredToSegment(corner, fromPoint, toPoint) > maxCornerOffset * maxCornerOffset)
            return false;

        float minLeg = gapLength * 0.08f;
        return corner.DistanceSquaredTo(fromPoint) >= minLeg * minLeg &&
               corner.DistanceSquaredTo(toPoint) >= minLeg * minLeg;
    }

    private static ImmutableArray<Vector2> BuildSmoothBridge(
        Vector2 fromPoint,
        Vector2 fromTangent,
        Vector2 toPoint,
        Vector2 toTangent)
    {
        float handleLength = fromPoint.DistanceTo(toPoint) / 3f;
        var p1 = fromPoint + fromTangent * handleLength;
        var p2 = toPoint + toTangent * handleLength;

        var builder = ImmutableArray.CreateBuilder<Vector2>(SmoothBridgeSegments + 1);
        for (int i = 0; i <= SmoothBridgeSegments; i++)
        {
            float u = (float)i / SmoothBridgeSegments;
            builder.Add(Cubic(fromPoint, p1, p2, toPoint, u));
        }
        return CleanPolyline(builder.ToArray());
    }

    private static Vector2 Cubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float mt = 1f - t;
        return mt * mt * mt * p0
               + 3f * mt * mt * t * p1
               + 3f * mt * t * t * p2
               + t * t * t * p3;
    }

    private static float SampleRadius(Entity curve, float t)
    {
        if (!TryGetGeometry(curve, out var geom))
            return AppPreference.StrokeWireframeRadius;

        var positions = geom.Positions.Value;
        var radii = geom.Radii.Value;
        if (radii.Length == positions.Length)
            return GapBridgeGeometry.SampleFloat(radii, t, AppPreference.StrokeWireframeRadius);
        return radii.Length > 0 ? radii[0] : AppPreference.StrokeWireframeRadius;
    }

    private static float SamplePressure(Entity curve, float t)
    {
        if (!TryGetGeometry(curve, out var geom))
            return 1f;

        var positions = geom.Positions.Value;
        var pressures = geom.Pressures.Value;
        if (pressures.Length == positions.Length)
            return GapBridgeGeometry.SampleFloat(pressures, t, 1f);
        return pressures.Length > 0 ? pressures[0] : 1f;
    }

    private static Vector2 SampleTilt(Entity curve, float t)
    {
        if (!TryGetGeometry(curve, out var geom))
            return Vector2.Zero;

        var positions = geom.Positions.Value;
        var tilts = geom.Tilts.Value;
        if (tilts.Length == positions.Length)
            return TrimGeometry.SampleVec2(tilts, t);
        return tilts.Length > 0 ? tilts[0] : Vector2.Zero;
    }

    private static bool TryGetGeometry(Entity curve, out PolylineGeometry geom)
    {
        geom = null;
        if (!curve.IsAlive || !curve.Has<PolylineGeometry>())
            return false;

        geom = curve.Get<PolylineGeometry>();
        return geom.Positions.Value.Length >= 2;
    }

    private static void AddUniqueNormalized(List<Vector2> result, Vector2 tangent)
    {
        tangent = NormalizeOrZero(tangent);
        if (tangent == Vector2.Zero)
            return;

        foreach (var existing in result)
        {
            if (existing.IsEqualApprox(tangent))
                return;
        }
        result.Add(tangent);
    }

    private static ImmutableArray<Vector2> CleanPolyline(params Vector2[] points)
    {
        var builder = ImmutableArray.CreateBuilder<Vector2>(points.Length);
        foreach (var point in points)
        {
            if (builder.Count == 0 || builder[^1].DistanceSquaredTo(point) > Epsilon * Epsilon)
                builder.Add(point);
        }
        return builder.ToImmutable();
    }

    private static Vector2 NormalizeOrZero(Vector2 v)
    {
        return v.LengthSquared() <= Epsilon * Epsilon ? Vector2.Zero : v.Normalized();
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq <= Epsilon * Epsilon)
            return point.DistanceSquaredTo(a);

        float t = Mathf.Clamp((point - a).Dot(ab) / lenSq, 0f, 1f);
        return point.DistanceSquaredTo(a + t * ab);
    }
}
