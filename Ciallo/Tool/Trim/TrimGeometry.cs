using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.Tool;

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
    // No coalescing of adjacent halfedge intervals: zero-length kept pieces produced between adjacent
    // halfedges are filtered later by the caller's bbox check.
    public static List<(float From, float To)> InvertDoomedRanges(
        IReadOnlyList<TrimEdgeHit> doomed, int sourceLength)
    {
        if (sourceLength < 2) return [];
        float maxT = sourceLength - 1f;

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
}
