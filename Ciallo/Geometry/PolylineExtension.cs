using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        // Memory allocation here and Rider warns quite a lot, but it's ok. 
        var searchResult = polyline.Select(v => v.X).ToImmutableArray().BinarySearch(x);
        if (searchResult >= 0) return polyline[searchResult].Y;
        // Get the index of the closest point after x
        // see https://learn.microsoft.com/en-us/dotnet/api/system.array.binarysearch for the return value.
        int idx = ~searchResult;
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
    public static List<float> SampleXList([NotNull] this IReadOnlyList<Vector2> polyline, IReadOnlyList<float> xs)
    {
        int count = polyline.Count;
        if (count == 0) throw new ArgumentException("Polyline cannot be empty.", nameof(polyline));
        var ys = new List<float>(xs.Count);
        if (count == 1)
        {
            float y0 = polyline[0].Y;
            for (int i = 0; i < xs.Count; i++)
                ys.Add(y0);
            return ys;
        }
        // For monotone-in-X polyline and sorted xs, scan segments in one pass
        int segIdx = 1;
        foreach (var x in xs)
        {
            // before first point
            if (x <= polyline[0].X)
            {
                ys.Add(polyline[0].Y);
                continue;
            }
            // after last point
            if (x >= polyline[count - 1].X)
            {
                ys.Add(polyline[count - 1].Y);
                continue;
            }
            // advance segment until x is within [prev.X, curr.X]
            while (segIdx < count - 1 && x > polyline[segIdx].X)
                segIdx++;
            var p0 = polyline[segIdx - 1];
            var p1 = polyline[segIdx];
            ys.Add(SampleSegment(p0, p1, x));
        }
        return ys;
    }

    /// <summary>
    /// The Visvalingam–Whyatt algorithm to simplify the polyline.
    /// Remove the smallest effective area points until the remaining point count reaches (ratio * count).
    /// ratio in [0,1] keeps that fraction of points (clamped). ratio >= 1 keeps all.
    /// </summary>
    /// <param name="polyline">The polyline to Simplify.</param>
    /// <param name="simplificationRatio">#points to remove divided by total #points.</param>
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
            if (n > 0 && n < count - 1 && n < count && !removed[n])
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
    /// Distance metric variant of the Visvalingam–Whyatt algorithm. Use perpendicular distance instead of triangle area to evaluate importance.
    /// Remove the smallest effective perpendicular distance points until the remaining point count reaches (ratio * count).
    /// </summary>
    /// <inheritdoc cref="SimplifyVm"/>
    public static List<Vector2> SimplifyH(this IReadOnlyList<Vector2> polyline, float simplificationRatio, out List<int> originalIndex)
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
        var distance = new float[count];
        for (int i = 0; i < count; i++)
        {
            prev[i] = i - 1;
            next[i] = i + 1;
        }
        next[count - 1] = count;

        float PerpendicularDistance(int i)
        {
            int p = prev[i];
            int n = next[i];
            if (p < 0 || n >= count) return float.PositiveInfinity;
            var dir = polyline[n] - polyline[p];
            if (dir.IsZeroApprox()) return float.PositiveInfinity;
            return polyline[i].DistanceToLine(polyline[p], dir.Normalized());
        }

        var heap = new PriorityQueue<int, float>();
        for (int i = 1; i < count - 1; i++)
        {
            distance[i] = PerpendicularDistance(i);
            heap.Enqueue(i, distance[i]);
        }

        int remaining = count;
        while (remaining > targetCount && heap.Count > 0)
        {
            var i = heap.Dequeue();
            if (removed[i]) continue;
            float currentDistance = PerpendicularDistance(i);
            if (MathF.Abs(currentDistance - distance[i]) > 1e-6f)
            {
                distance[i] = currentDistance;
                heap.Enqueue(i, distance[i]);
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
                distance[p] = PerpendicularDistance(p);
                heap.Enqueue(p, distance[p]);
            }
            if (n > 0 && n < count - 1 && !removed[n])
            {
                distance[n] = PerpendicularDistance(n);
                heap.Enqueue(n, distance[n]);
            }
        }

        List<Vector2> result = [];
        originalIndex = [];
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
}