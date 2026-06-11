using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Geometry;
using Frent;
using Godot;
using Godot.Collections;

namespace Ciallo.Tool;

// Native Arrangement2D.polyline_query_edges returns one dict per crossed halfedge:
// { source_id: long (Entity.PackedValue), from_t: float, to_t: float }.
// from_t/to_t are fractional indices into the source polyline's segment array.
public readonly record struct TrimEdgeHit(Entity SourceShape, float FromT, float ToT);

// Pure helpers shared by TrimInteractor (preview) and TrimTool commit logic.
public static class TrimGeometry
{
    public static List<TrimEdgeHit> ParseEdgeHits(Array<Dictionary> raw)
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
        // adjacent halfedges. Overlapping halfedges from x-monotone splits or repeated edges do
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
                float length = sourcePositions.GetLength(iv.From, iv.To);
                if (length <= undercutDistance * 2f)
                    continue;

                float from = iv.From <= 0f
                    ? iv.From
                    : sourcePositions.MoveTByDistance(iv.From, undercutDistance, forward: true);
                float to = iv.To >= maxT
                    ? iv.To
                    : sourcePositions.MoveTByDistance(iv.To, undercutDistance, forward: false);
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
}
