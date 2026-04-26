using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// This class is for Polyline geometry/math calculations. Without any curve modification and cache logic.
/// </summary>
public static class PolylineExtension
{
    public static Rect2 GetBoundingBox([NotNull] this IReadOnlyList<Vector2> polyline, IReadOnlyList<float> radii = null)
    {
        int count = polyline.Count;
        if (count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        var radiiList = radii ?? Enumerable.Repeat(0f, count).ToList();
        Vector2 first = polyline[0];
        float firstR = radiiList[0];
        float minX = first.X - firstR;
        float minY = first.Y - firstR;
        float maxX = first.X + firstR;
        float maxY = first.Y + firstR;

        for (int i = 1; i < count; i++)
        {
            Vector2 p = polyline[i];
            float r = radiiList[i];
            float xMin = p.X - r;
            float yMin = p.Y - r;
            float xMax = p.X + r;
            float yMax = p.Y + r;
            if (xMin < minX) minX = xMin;
            if (yMin < minY) minY = yMin;
            if (xMax > maxX) maxX = xMax;
            if (yMax > maxY) maxY = yMax;
        }

        return new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
    }

    /// <summary>
    /// Find the closest point on the polyline to the given point.
    /// </summary>
    /// <returns>The closest point position and output its fractional t.</returns>
    public static Vector2 GetClosestPoint([NotNull] this IReadOnlyList<Vector2> polyline, Vector2 point, out float t)
    {
        t = 0f;
        int count = polyline.Count;
        if (count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        if (count == 1) return polyline[0];

        Vector2 closestPoint = polyline[0];
        float minDistanceSq = point.DistanceSquaredTo(closestPoint);

        for (int i = 1; i < count; i++)
        {
            Vector2 p1 = polyline[i - 1];
            Vector2 p2 = polyline[i];
            Vector2 segment = p2 - p1;
            float segmentLengthSq = segment.LengthSquared();

            Vector2 currentClosest;
            float currentT;
            if (segmentLengthSq < 1e-5f)
            {
                currentClosest = p1;
                currentT = 0f;
            }
            else
            {
                var s = (point - p1).Dot(segment) / segmentLengthSq;
                if (s < 0f)
                {
                    currentClosest = p1;
                    currentT = 0f;
                }
                else if (s > 1f)
                {
                    currentClosest = p2;
                    currentT = 1f;
                }
                else
                {
                    currentClosest = p1 + s * segment;
                    currentT = s;
                }
            }

            var distSq = point.DistanceSquaredTo(currentClosest);
            if (!(distSq < minDistanceSq)) continue;
            minDistanceSq = distSq;
            closestPoint = currentClosest;
            t = i - 1 + currentT;
        }

        return closestPoint;
    }

    /// <summary>
    /// Batch version of <see cref="GetClosestPoint(IReadOnlyList{Vector2},Vector2,out float)"/>.
    /// Assumes consecutive query points are spatially adjacent and the polyline has no self-intersections.
    /// Uses a sliding cursor with a miss tolerance so the total work is O(M+N) amortized.
    /// Corner cases (e.g. sharp U-turns) may produce slightly wrong results.
    /// </summary>
    /// <remarks>Sonnet4.6 gen</remarks>
    /// <param name="polyline">The reference polyline (no self-intersections).</param>
    /// <param name="points">Query points; spatially adjacent pairs must be consecutively ordered.</param>
    /// <param name="missLimit">
    /// After distance starts increasing, keep probing this many extra segments before giving up.
    /// Applied symmetrically to both forward and backward directions.
    /// </param>
    public static (Vector2[] closestPoints, float[] polyTs) GetClosestPoint(
        [NotNull] this IReadOnlyList<Vector2> polyline,
        IReadOnlyList<Vector2> points,
        int missLimit = 4)
    {
        int n = points.Count;
        var closestPoints = new Vector2[n];
        var polyTs = new float[n];

        int segCount = polyline.Count - 1; // number of segments
        if (polyline.Count == 0 || n == 0) return (closestPoints, polyTs);
        if (polyline.Count == 1)
        {
            for (int i = 0; i < n; i++) closestPoints[i] = polyline[0];
            return (closestPoints, polyTs);
        }

        // Projects point onto segment [p1,p2], returns dist² and sets localT ∈ [0,1].
        static float ProjectOntoSegment(Vector2 p1, Vector2 p2, Vector2 point, out float localT)
        {
            Vector2 seg = p2 - p1;
            float lenSq = seg.LengthSquared();
            if (lenSq < 1e-10f)
            {
                localT = 0f;
                return point.DistanceSquaredTo(p1);
            }
            float s = (point - p1).Dot(seg) / lenSq;
            localT = Math.Clamp(s, 0f, 1f);
            return point.DistanceSquaredTo(p1 + localT * seg);
        }

        // Full scan for the first query point to initialise the cursor.
        float bestDistSq = float.MaxValue;
        int curSeg = 0;
        float bestLocalT = 0f;
        for (int s = 0; s < segCount; s++)
        {
            float dSq = ProjectOntoSegment(polyline[s], polyline[s + 1], points[0], out float lt);
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                curSeg = s;
                bestLocalT = lt;
            }
        }
        closestPoints[0] = polyline[curSeg] + bestLocalT * (polyline[curSeg + 1] - polyline[curSeg]);
        polyTs[0] = curSeg + bestLocalT;

        // Slide the cursor for subsequent points.
        for (int i = 1; i < n; i++)
        {
            Vector2 pt = points[i];
            bestDistSq = ProjectOntoSegment(polyline[curSeg], polyline[curSeg + 1], pt, out bestLocalT);
            int bestSeg = curSeg;

            // Probe forward
            int misses = 0;
            for (int s = curSeg + 1; s < segCount && misses < missLimit; s++)
            {
                float dSq = ProjectOntoSegment(polyline[s], polyline[s + 1], pt, out float lt);
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestSeg = s;
                    bestLocalT = lt;
                    misses = 0;
                }
                else misses++;
            }

            // Probe backward
            misses = 0;
            for (int s = curSeg - 1; s >= 0 && misses < missLimit; s--)
            {
                float dSq = ProjectOntoSegment(polyline[s], polyline[s + 1], pt, out float lt);
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestSeg = s;
                    bestLocalT = lt;
                    misses = 0;
                }
                else misses++;
            }

            curSeg = bestSeg;
            closestPoints[i] = polyline[curSeg] + bestLocalT * (polyline[curSeg + 1] - polyline[curSeg]);
            polyTs[i] = curSeg + bestLocalT;
        }

        return (closestPoints, polyTs);
    }

    /// <summary>
    /// Check if the curve is a monotone in X.
    /// So that each element of the function's domain X maps to a single, well-defined element of its range Y.
    /// </summary>
    public static bool IsXMonotone([NotNull] this IReadOnlyList<Vector2> polyline)
    {
        if (polyline.Count < 2) return true;
        var sign = MathF.Sign(polyline[1].X - polyline[0].X);
        if (sign == 0) return false;
        if (polyline.Count == 2) return true;
        return Enumerable.Range(1, polyline.Count - 1).All(i => MathF.Sign(polyline[i - 1].X - polyline[i].X) == sign);
    }

    /// <summary>
    /// Sample the polyline at the given x value, assuming the polyline is x-monotone.
    /// If x is out of bound, it will return the Y value of the start/end point.
    /// </summary>
    /// <param name="polyline"></param>
    /// <param name="x"></param>
    /// <returns>Y value at the given x</returns>
    public static float SampleX([NotNull] this IReadOnlyList<Vector2> polyline, float x)
    {
        if (polyline.Count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        if (polyline.Count == 1) return polyline[0].Y;
        if (polyline.Count == 2) return SampleSegment(polyline[0], polyline[1], x);

        int lo = 0, hi = polyline.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = polyline[mid].X.CompareTo(x);
            if (cmp < 0) lo = mid + 1;
            else if (cmp > 0) hi = mid - 1;
            else return polyline[mid].Y; // exact match
        }
        // lo is the insertion point: first index where polyline[lo].X > x
        int idx = lo;
        if (idx == 0) return polyline[0].Y;
        if (idx == polyline.Count) return polyline[^1].Y;
        return SampleSegment(polyline[idx - 1], polyline[idx], x);
    }

    private static float SampleSegment(Vector2 p0, Vector2 p1, float xValue)
    {
        if (xValue < p0.X) return p0.Y;
        if (xValue > p1.X) return p1.Y;
        if (MathF.Abs(p1.X - p0.X) < 1e-5f)
            return (p0.Y + p1.Y) / 2; // vertical
        float slope = (p1.Y - p0.Y) / (p1.X - p0.X);
        return p0.Y + slope * (xValue - p0.X);
    }

    /// <summary>
    /// Sample the polyline by the given x ordered list from small to large.
    /// </summary>
    public static float[] SampleXList([NotNull] this IReadOnlyList<Vector2> polyline, IReadOnlyList<float> xs)
    {
        int count = polyline.Count;
        if (count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        var ys = new float[xs.Count];
        if (count == 1)
        {
            float y0 = polyline[0].Y;
            for (int i = 0; i < xs.Count; i++)
                ys[i] = y0;
            return ys;
        }
        // For monotone-in-X polyline and sorted xs, scan segments in one pass
        int segIdx = 1;
        for (int i = 0; i < xs.Count; i++)
        {
            float x = xs[i];
            // before first point
            if (x <= polyline[0].X)
            {
                ys[i] = polyline[0].Y;
                continue;
            }
            // after last point
            if (x >= polyline[count - 1].X)
            {
                ys[i] = polyline[count - 1].Y;
                continue;
            }
            // advance segment until x is within [prev.X, curr.X]
            while (segIdx < count - 1 && x > polyline[segIdx].X)
                segIdx++;
            ys[i] = SampleSegment(polyline[segIdx - 1], polyline[segIdx], x);
        }
        return ys;
    }

    /// <summary>
    /// The Visvalingam–Whyatt algorithm to simplify the polyline.
    /// Remove the smallest effective area points until the remaining point count reaches (ratio * count).
    /// ratio in [0,1] keeps that fraction of points (clamped). ratio >= 1 keeps all.
    /// </summary>
    /// <remarks>
    /// This algorithm does not fit well for our polylines, whose points are dense in turnings/corners and sparse in straight segments.
    /// It tends to remove less points in straight segments and more in corners, which is opposite to our need.
    /// </remarks>
    /// <param name="polyline">The polyline to Simplify.</param>
    /// <param name="simplificationRatio">Fraction of points to remove, in [0,1]</param>
    /// <param name="originalIndex">The output point indices in the original polyline.</param>
    /// <returns>Simplified polyline.</returns>
    public static List<Vector2> SimplifyVm(this IReadOnlyList<Vector2> polyline, float simplificationRatio, out List<int> originalIndex)
    {
        int count = polyline.Count;
        float ratio = 1f - simplificationRatio;
        if (count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        if (count <= 2 || ratio >= 1f)
        {
            originalIndex = Enumerable.Range(0, count).ToList();
            return polyline.ToList();
        }
        if (ratio <= 0f)
            ratio = 0f; // keep minimum 2 points anyway

        int targetCount = (int)MathF.Round(count * ratio);
        if (targetCount < 2) targetCount = 2;
        if (targetCount > count) targetCount = count;
        if (targetCount == count)
        {
            originalIndex = Enumerable.Range(0, count).ToList();
            return polyline.ToList();
        }

        // Node arrays (index-based linked list)
        var prev = new int[count];
        var next = new int[count];
        var removed = new bool[count];
        var area = new float[count];
        for (int i = 0; i < count; i++)
        {
            prev[i] = i - 1;
            next[i] = i + 1;
        }
        next[count - 1] = count; // sentinel > last index

        float TriangleArea(int i)
        {
            int p = prev[i];
            int n = next[i];
            if (p < 0 || n >= count) return float.PositiveInfinity; // endpoints not removable
            return Geometry.TriangleArea(polyline[p], polyline[i], polyline[n]);
        }

        var heap = new PriorityQueue<int, float>();
        for (int i = 1; i < count - 1; i++)
        {
            area[i] = TriangleArea(i);
            heap.Enqueue(i, area[i]);
        }

        int remaining = count;
        // Remove until desired count
        while (remaining > targetCount && heap.Count > 0)
        {
            var i = heap.Dequeue();
            if (removed[i]) continue; // already gone by a newer entry
            // stale entry check (priority queue lacks decrease-key)
            float currentArea = TriangleArea(i);
            if (MathF.Abs(currentArea - area[i]) > 1e-6f)
            {
                // area changed since enqueued; re-enqueue with updated value
                area[i] = currentArea;
                heap.Enqueue(i, area[i]);
                continue;
            }

            // Remove this point
            removed[i] = true;
            remaining--;
            int p = prev[i];
            int n = next[i];
            if (p >= 0) next[p] = n;
            if (n < count) prev[n] = p;

            // Update neighbor areas (if they are not endpoints and not removed)
            if (p > 0 && p < count - 1 && !removed[p])
            {
                area[p] = TriangleArea(p);
                heap.Enqueue(p, area[p]);
            }
            if (n > 0 && n < count - 1 && !removed[n])
            {
                area[n] = TriangleArea(n);
                heap.Enqueue(n, area[n]);
            }
        }

        // Collect remaining points in order
        List<Vector2> result = [];
        originalIndex = [];
        int idx = 0;
        while (idx < count) // simple traversal from start
        {
            if (!removed[idx])
            {
                result.Add(polyline[idx]);
                originalIndex.Add(idx);
            }
            idx = next[idx];
            if (idx >= count) break;
        }

        return result;
    }

    /// <summary>
    /// A variant of Visvalingam–Whyatt with Curvature-weighted distance metric.
    /// Prefer removing points on straight segments, keep dense points in corners.
    /// </summary>
    /// <param name="polyline">Input polyline points.</param>
    /// <param name="simplificationRatio">
    /// Fraction of points to remove, in [0,1].
    /// 0 keeps original, 1 leaves the minimum (2) points.
    /// </param>
    /// <param name="originalIndex">Indices of kept points in the original polyline.</param>
    /// <returns>Simplified polyline.</returns>
    public static List<Vector2> SimplifyCurvatureDistance(this IReadOnlyList<Vector2> polyline, float simplificationRatio, out List<int> originalIndex)
    {
        int count = polyline.Count;
        float ratio = 1f - simplificationRatio;

        if (count <= 2 || ratio >= 1f)
        {
            originalIndex = Enumerable.Range(0, count).ToList();
            return polyline.ToList();
        }

        if (ratio <= 0f)
            ratio = 0f;

        int targetCount = (int)MathF.Round(count * ratio);
        if (targetCount < 2) targetCount = 2;
        if (targetCount > count) targetCount = count;
        if (targetCount == count)
        {
            originalIndex = Enumerable.Range(0, count).ToList();
            return polyline.ToList();
        }

        var prev = new int[count];
        var next = new int[count];
        var removed = new bool[count];
        var importance = new float[count];

        for (int i = 0; i < count; i++)
        {
            prev[i] = i - 1;
            next[i] = i + 1;
        }

        next[count - 1] = count;

        float PointImportance(int i)
        {
            int p = prev[i];
            int n = next[i];
            if (p < 0 || n >= count) return float.PositiveInfinity;

            var a = polyline[p];
            var b = polyline[i];
            var c = polyline[n];

            var ab = b - a;
            var bc = c - b;
            var ac = c - a;

            if (ab.IsZeroApprox() || bc.IsZeroApprox() || ac.IsZeroApprox())
                return float.PositiveInfinity;

            // perpendicular distance from b to line a-c
            float dist = b.DistanceToLine(a, ac.Normalized());

            return dist / Mathf.Log(Mathf.Max(1e-5f, ab.Length() + bc.Length()));
        }

        var heap = new PriorityQueue<int, float>();
        for (int i = 1; i < count - 1; i++)
        {
            importance[i] = PointImportance(i);
            heap.Enqueue(i, importance[i]);
        }

        int remaining = count;

        while (remaining > targetCount && heap.Count > 0)
        {
            int i = heap.Dequeue();
            if (removed[i]) continue;

            float currentImportance = PointImportance(i);
            if (MathF.Abs(currentImportance - importance[i]) > 1e-6f)
            {
                importance[i] = currentImportance;
                heap.Enqueue(i, importance[i]);
                continue;
            }

            removed[i] = true;
            remaining--;

            int p = prev[i];
            int n = next[i];

            if (p >= 0) next[p] = n;
            if (n < count) prev[n] = p;

            if (p > 0 && p < count - 1 && !removed[p])
            {
                importance[p] = PointImportance(p);
                heap.Enqueue(p, importance[p]);
            }

            if (n > 0 && n < count - 1 && !removed[n])
            {
                importance[n] = PointImportance(n);
                heap.Enqueue(n, importance[n]);
            }
        }

        var result = new List<Vector2>();
        originalIndex = new List<int>();
        int idx = 0;

        while (idx < count)
        {
            if (!removed[idx])
            {
                result.Add(polyline[idx]);
                originalIndex.Add(idx);
            }

            idx = next[idx];
            if (idx >= count) break;
        }

        return result;
    }

    /// <summary>
    /// Turn a polyline into a simple polygon.
    /// A simple polygon is a closed polygon that does not intersect itself (so no overlapping points).
    /// </summary>
    /// <remarks>Deal closed polygon incorrectly</remarks>
    public static List<Vector2> ToSimplePolygon(this IReadOnlyList<Vector2> polyline)
    {
        int i = polyline.FindFirstSelfIntersection(out _);
        if (i == -1) return polyline.RemoveDuplicatePoints();
        return polyline.Take(i + 1).ToArray().RemoveDuplicatePoints();
    }

    public static List<Vector2> RemoveDuplicatePoints(this IReadOnlyList<Vector2> polyline)
    {
        if (polyline.Count == 0) return new List<Vector2>();
        List<Vector2> result = [polyline[0]];
        for (int i = 1; i < polyline.Count; i++)
        {
            if (!polyline[i].IsEqualApprox(polyline[i - 1]))
                result.Add(polyline[i]);
        }
        return result;
    }

    // Treat input as a polygon without requiring polygon[^1] == polygon[0].
    public static int FindFirstSelfIntersection(this IReadOnlyList<Vector2> polygon, out Vector2 intersectionPoint)
    {
        intersectionPoint = Vector2.Zero;
        int count = polygon.Count;
        if (count < 4) return -1;

        for (int i = 0; i < count; i++)
        {
            int iNext = (i + 1) % count;
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[iNext];

            // Start j two edges ahead to avoid adjacent edges and shared vertices.
            int jStart = (i + 2) % count;
            int j = jStart;
            while (true)
            {
                int jNext = (j + 1) % count;

                // Skip if edges are the same or neighbors (share a vertex)
                if (j == i || j == iNext || jNext == i || jNext == iNext)
                {
                    j = (j + 1) % count;
                    if (j == jStart) break;
                    continue;
                }

                Vector2 p3 = polygon[j];
                Vector2 p4 = polygon[jNext];

                var intersection = Geometry.SegmentIntersect(p1, p2, p3, p4);
                if (intersection.HasValue)
                {
                    intersectionPoint = intersection.Value;
                    // Return the index of the first edge's start point that intersects.
                    return i;
                }

                j = (j + 1) % count;
                if (j == jStart) break;
            }
        }

        return -1;
    }

    public static Vector2[] SmoothLaplacian(this IReadOnlyList<Vector2> polyline, int iterations, float lambda)
    {
        int count = polyline.Count;
        if (count < 3 || iterations <= 0) return polyline.ToArray();

        var smoothed = polyline.ToArray();

        for (int iter = 0; iter < iterations; iter++)
        {
            Vector2[] newPositions = [..smoothed];

            for (int i = 1; i < count - 1; i++)
            {
                Vector2 prev = smoothed[i - 1];
                Vector2 curr = smoothed[i];
                Vector2 next = smoothed[i + 1];

                Vector2 laplacian = (prev + next) / 2 - curr;
                newPositions[i] = curr + lambda * laplacian;
            }

            smoothed = newPositions;
        }

        return smoothed;
    }
}