using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Geometry;

public static class ConnectPolygonHoles
{
    /// <summary>
    /// Merges holes into the outer polygon by inserting bridge edges.
    /// The result is a single simple polygon (with duplicated bridge vertices) that
    /// can be triangulated without special hole handling.
    /// Should behave identically to CGAL/connect_holes.h.
    /// </summary>
    /// <param name="polygonWithHoles">First array is outer polygon (CCW), other arrays are holes (CW)</param>
    /// <returns>Single merged polygon ring</returns>
    public static List<Vector2> ConnectHoles(this IReadOnlyList<IReadOnlyList<Vector2>> polygonWithHoles)
    {
        // CC Sonnet 4.6 gen
        // Start with a mutable copy of the outer polygon.
        var merged = new List<Vector2>(polygonWithHoles[0]);

        for (int h = 1; h < polygonWithHoles.Count; h++)
        {
            var hole = polygonWithHoles[h];
            if (hole.Count == 0) continue;
            merged = ConnectOneHole(merged, hole);
        }

        return merged;
    }

    /// <summary>
    /// Connect a single hole into the current outer ring and return the merged ring.
    /// </summary>
    private static List<Vector2> ConnectOneHole(List<Vector2> outer, IReadOnlyList<Vector2> hole)
    {
        // Step 1: find the rightmost vertex of the hole.
        int holeMaxIdx = 0;
        for (int i = 1; i < hole.Count; i++)
        {
            if (hole[i].X > hole[holeMaxIdx].X ||
                (MathF.Abs(hole[i].X - hole[holeMaxIdx].X) < 1e-6f && hole[i].Y < hole[holeMaxIdx].Y))
                holeMaxIdx = i;
        }
        Vector2 holeVtx = hole[holeMaxIdx];

        // Step 2: cast a ray from holeVtx in the +X direction and find the closest
        //         intersection with any edge of the outer ring.
        int outerEdgeIdx = -1; // start index of the best outer edge
        float bestT = float.MaxValue; // parameter along the outer edge
        float bestX = float.MaxValue; // x of the intersection

        int outerCount = outer.Count;
        for (int i = 0; i < outerCount; i++)
        {
            Vector2 a = outer[i];
            Vector2 b = outer[(i + 1) % outerCount];

            // Only consider edges whose y-range straddles holeVtx.Y
            // (strictly to avoid double-counting shared vertices)
            float minY = MathF.Min(a.Y, b.Y);
            float maxY = MathF.Max(a.Y, b.Y);
            if (holeVtx.Y < minY || holeVtx.Y >= maxY) continue;

            // Compute x of the intersection of the edge with the horizontal ray y = holeVtx.Y
            float dy = b.Y - a.Y;
            float t = (holeVtx.Y - a.Y) / dy;
            float xIntersect = a.X + t * (b.X - a.X);

            // Only intersections to the right of (or exactly at) holeVtx
            if (xIntersect < holeVtx.X - 1e-6f) continue;

            if (xIntersect < bestX)
            {
                bestX = xIntersect;
                bestT = t;
                outerEdgeIdx = i;
            }
        }

        // Step 3: determine the insertion point on the outer ring.
        // If the intersection is exactly a vertex of the outer ring, use that vertex.
        // Otherwise, split the edge by inserting the intersection point, then use it.
        int insertIdx; // index in `outer` of the point we will bridge to

        int nextOuter = (outerEdgeIdx + 1) % outerCount;
        // Is the intersection exactly the end-vertex of the edge?
        if (MathF.Abs(bestT - 1f) < 1e-6f)
        {
            insertIdx = nextOuter;
        }
        // Is the intersection exactly the start-vertex?
        else if (MathF.Abs(bestT) < 1e-6f)
        {
            insertIdx = outerEdgeIdx;
        }
        else
        {
            // Insert the intersection point into the outer ring.
            Vector2 intersectPt = new Vector2(bestX, holeVtx.Y);
            outer.Insert(nextOuter, intersectPt);
            insertIdx = nextOuter;
        }

        // Step 4: build the merged ring.
        // The bridge goes:  ... outer[0..insertIdx], holeVtx, hole (rotated), holeVtx, outer[insertIdx], ...
        // i.e. we insert the hole starting at holeMaxIdx and wrap around back to holeMaxIdx,
        // then repeat outer[insertIdx] to close the bridge.
        var result = new List<Vector2>(outer.Count + hole.Count + 2);

        // Outer vertices up to and including the bridge point
        for (int i = 0; i <= insertIdx; i++)
            result.Add(outer[i]);

        // Hole vertices starting from holeMaxIdx, going around the whole hole
        for (int i = 0; i <= hole.Count; i++)
            result.Add(hole[(holeMaxIdx + i) % hole.Count]);

        // Bridge back: repeat the outer bridge point
        result.Add(outer[insertIdx]);

        // Remaining outer vertices
        for (int i = insertIdx + 1; i < outer.Count; i++)
            result.Add(outer[i]);

        return result;
    }
}