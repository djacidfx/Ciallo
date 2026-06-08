using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.Tool;

// Native Arrangement2D.polyline_query_edges returns one dict per crossed halfedge:
// { source_id: long (Entity.PackedValue), from_t: float, to_t: float }.
// from_t/to_t are fractional indices into the source polyline's segment array.
public readonly record struct TrimEdgeHit(Entity SourceShape, float FromT, float ToT);

// Pure helpers shared by TrimInteractor (preview) and TrimTool commit logic.
public static class TrimGeometry
{
    public static List<TrimEdgeHit> ParseEdgeHits(Godot.Collections.Array<Godot.Collections.Dictionary> raw)
    {
        var hits = new List<TrimEdgeHit>(raw.Count);
        foreach (var dict in raw)
        {
            long sourceId = (long)dict["source_id"];
            float fromT = (float)dict["from_t"];
            float toT = (float)dict["to_t"];
            hits.Add(new TrimEdgeHit(sourceId.ToEntity(), fromT, toT));
        }
        return hits;
    }

    // Sub-slice an array of element type T by t ∈ [0, count-1] using linear interpolation
    // at the two ends and verbatim copy of integer-indexed elements in between.
    public static ImmutableArray<Vector2> SliceVec2(ImmutableArray<Vector2> src, float fromT, float toT)
        => Slice(src, fromT, toT, LerpVec2);

    public static ImmutableArray<float> SliceFloat(ImmutableArray<float> src, float fromT, float toT)
        => Slice(src, fromT, toT, (a, b, t) => a + (b - a) * t);

    private static ImmutableArray<T> Slice<T>(
        ImmutableArray<T> src, float fromT, float toT, Func<T, T, float, T> lerp)
    {
        if (src.Length == 0) return [];
        int n = src.Length;
        fromT = Math.Clamp(fromT, 0f, n - 1f);
        toT = Math.Clamp(toT, 0f, n - 1f);
        if (toT < fromT) (fromT, toT) = (toT, fromT);

        int firstWhole = (int)Math.Ceiling(fromT);
        int lastWhole = (int)Math.Floor(toT);

        var builder = ImmutableArray.CreateBuilder<T>();

        // Leading interpolated point (only if fromT is not exactly on an integer index).
        if (firstWhole > fromT + 1e-6f)
        {
            int i = firstWhole - 1;
            float local = fromT - i;
            builder.Add(lerp(src[i], src[i + 1], local));
        }

        for (int i = firstWhole; i <= lastWhole && i < n; i++)
            builder.Add(src[i]);

        // Trailing interpolated point.
        if (lastWhole < toT - 1e-6f && lastWhole + 1 < n)
        {
            float local = toT - lastWhole;
            builder.Add(lerp(src[lastWhole], src[lastWhole + 1], local));
        }

        return builder.ToImmutable();
    }

    private static Vector2 LerpVec2(Vector2 a, Vector2 b, float t) => a.Lerp(b, t);

    // Build the kept ranges for a single source from a collection of doomed (from_t, to_t) intervals.
    // The doomed intervals are merged first, then undercut in world space. This keeps a tiny amount
    // of original geometry around intersections so the rebuilt arrangement still feels connected.
    // The goal is apparent correctness for drawing, not exact graph-topology preservation.
    public static List<(float From, float To)> InvertDoomedRanges(
        IReadOnlyList<TrimEdgeHit> doomed,
        ImmutableArray<Vector2> sourcePositions,
        float undercutDistance)
    {
        if (sourcePositions.Length < 2) return [];
        float maxT = sourcePositions.Length - 1f;

        // Normalize and sort.
        var intervals = new List<(float From, float To)>(doomed.Count);
        foreach (var h in doomed)
        {
            float a = Math.Clamp(Math.Min(h.FromT, h.ToT), 0f, maxT);
            float b = Math.Clamp(Math.Max(h.FromT, h.ToT), 0f, maxT);
            if (b > a) intervals.Add((a, b));
        }
        intervals.Sort((x, y) => x.From.CompareTo(y.From));

        // Merge overlapping doomed intervals (this is required for correctness, not coalescing of
        // adjacent halfedges — overlapping halfedges from x-monotone splits or repeated edges do
        // happen and would otherwise produce bogus kept pieces).
        var merged = new List<(float From, float To)>();
        foreach (var iv in intervals)
        {
            if (merged.Count > 0 && iv.From <= merged[^1].To)
            {
                var last = merged[^1];
                merged[^1] = (last.From, Math.Max(last.To, iv.To));
            }
            else
            {
                merged.Add(iv);
            }
        }

        if (undercutDistance > 0f)
        {
            for (int i = 0; i < merged.Count; i++)
            {
                var iv = merged[i];
                float length = GetPolylineLength(sourcePositions, iv.From, iv.To);
                if (length <= undercutDistance * 2f)
                    continue;

                float from = iv.From <= 0f
                    ? iv.From
                    : MoveTByWorldDistance(sourcePositions, iv.From, undercutDistance, forward: true);
                float to = iv.To >= maxT
                    ? iv.To
                    : MoveTByWorldDistance(sourcePositions, iv.To, undercutDistance, forward: false);
                if (to > from)
                    merged[i] = (from, to);
            }
        }

        // Invert.
        var kept = new List<(float From, float To)>();
        float cursor = 0f;
        foreach (var iv in merged)
        {
            if (iv.From > cursor) kept.Add((cursor, iv.From));
            cursor = iv.To;
        }
        if (maxT > cursor) kept.Add((cursor, maxT));
        return kept;
    }

    public static float GetPolylineLength(IReadOnlyList<Vector2> polyline)
    {
        float length = 0f;
        for (int i = 1; i < polyline.Count; i++)
            length += polyline[i - 1].DistanceTo(polyline[i]);
        return length;
    }

    private static float GetPolylineLength(ImmutableArray<Vector2> polyline, float fromT, float toT)
    {
        if (polyline.Length < 2) return 0f;
        fromT = Math.Clamp(fromT, 0f, polyline.Length - 1f);
        toT = Math.Clamp(toT, 0f, polyline.Length - 1f);
        if (toT < fromT) (fromT, toT) = (toT, fromT);
        if (Math.Abs(toT - fromT) < 1e-6f) return 0f;

        float length = 0f;
        float t = fromT;
        while (t < toT)
        {
            int seg = Math.Min((int)MathF.Floor(t), polyline.Length - 2);
            float nextT = Math.Min(seg + 1f, toT);
            float segLen = polyline[seg].DistanceTo(polyline[seg + 1]);
            length += (nextT - t) * segLen;
            t = nextT;
        }
        return length;
    }

    private static float MoveTByWorldDistance(
        ImmutableArray<Vector2> polyline,
        float t,
        float distance,
        bool forward)
    {
        if (distance <= 0f || polyline.Length < 2) return t;
        float maxT = polyline.Length - 1f;
        t = Math.Clamp(t, 0f, maxT);

        while (distance > 0f)
        {
            int seg = forward
                ? Math.Min((int)MathF.Floor(t), polyline.Length - 2)
                : Math.Min((int)MathF.Ceiling(t) - 1, polyline.Length - 2);
            if (seg < 0 || seg >= polyline.Length - 1)
                return forward ? maxT : 0f;

            float segLen = polyline[seg].DistanceTo(polyline[seg + 1]);
            if (segLen <= 1e-6f)
            {
                t = forward ? seg + 1f : seg;
                continue;
            }

            float local = t - seg;
            float available = forward ? (1f - local) * segLen : local * segLen;
            if (distance <= available)
                return t + (forward ? 1f : -1f) * distance / segLen;

            distance -= available;
            t = forward ? seg + 1f : seg;
        }

        return t;
    }
}
