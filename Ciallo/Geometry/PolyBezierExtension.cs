using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Godot;
using Microsoft.Extensions.Caching.Memory;

namespace Ciallo.Geometry;

/// <summary>
/// Hold all the pure geometry/math calculations for PolyCubicBezier.
/// </summary>
public static class PolyBezierExtension
{
    private static readonly MemoryCache TessCache = new(new MemoryCacheOptions());

    [Pure] public static Rect2 GetBoundingBox(this IReadOnlyList<BezierPoint> points)
    {
        if (points == null || points.Count == 0) throw new InvalidOperationException("Cannot compute bounding box of an empty PolyCubicBezier.");
        if (points.Count == 1) return new(points[0].P, Vector2.Zero);

        Vector2 min = Vector2.Inf;
        Vector2 max = -Vector2.Inf;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = points[i].P;
            Vector2 p1 = points[i].Out + p0;
            Vector2 p3 = points[i + 1].P;
            Vector2 p2 = points[i + 1].In + p3;

            Vector2 a = 3 * (p1 - p0) - 6 * (p2 - p1) + 3 * (p3 - p2);
            Vector2 b = -6 * (p1 - p0) + 6 * (p2 - p1);
            Vector2 c = 3 * (p1 - p0);
            float[] ts = [..GetExtreme(a.X, b.X, c.X), ..GetExtreme(a.Y, b.Y, c.Y)];
            foreach (float t in ts)
            {
                if (t < 0 || t > 1) continue;
                Vector2 point = p0.BezierInterpolate(p1, p2, p3, t);
                min = min.Min(point);
                max = max.Max(point);
            }
            min = min.Min(p0).Min(p3);
            max = max.Max(p0).Max(p3);
        }
        return new(min, max - min);

        // Return an array of t values where the derivative is zero and within [0, 1]
        float[] GetExtreme(float a, float b, float c)
        {
            if (MathF.Abs(a) < 1e-7f)
            {
                if (MathF.Abs(b) < 1e-7f)
                    return [];
                float t = -c / b;
                return [t];
            }
            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
                return [];
            float sqrtD = MathF.Sqrt(discriminant);
            float t1 = (-b + sqrtD) / (2 * a);
            float t2 = (-b - sqrtD) / (2 * a);
            return [t1, t2];
        }
    }

    [Pure] public static bool IsXMonotone(this IReadOnlyList<BezierPoint> points)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = points[i].P;
            Vector2 p1 = points[i].Out + p0;
            Vector2 p3 = points[i + 1].P;
            Vector2 p2 = points[i + 1].In + p3;

            Vector2 a = 3 * (p1 - p0) - 6 * (p2 - p1) + 3 * (p3 - p2);
            Vector2 b = -6 * (p1 - p0) + 6 * (p2 - p1);
            Vector2 c = 3 * (p1 - p0);

            float ax = a.X;
            float bx = b.X;
            float cx = c.X;

            if (MathF.Abs(ax) < 1e-5f)
            {
                // linear case: bx·t + cx = 0 ⇒ t = -cx/bx
                if (MathF.Abs(bx) < 1e-5f)
                    continue; // constant derivative, no sign change
                float t = -cx / bx;
                if (t > 0 && t < 1)
                    return false;
            }
            else
            {
                float disc = bx * bx - 4 * ax * cx;
                if (disc <= 1e-5f)
                    continue; // no real or double root ⇒ no sign change
                float sqrtD = MathF.Sqrt(disc);
                float t1 = (-bx + sqrtD) / (2 * ax);
                float t2 = (-bx - sqrtD) / (2 * ax);
                if ((t1 > 0 && t1 < 1) || (t2 > 0 && t2 < 1))
                    return false;
            }
        }
        return true;
    }

    [Pure] public static (Vector2, BezierPoint, Vector2) CalculateInsertPoint(
        this IReadOnlyList<BezierPoint> points, int idx, float t)
    {
        // original segment endpoints and controls
        var left = points[idx];
        var right = points[idx + 1];
        Vector2 p0 = left.P;
        Vector2 p1 = p0 + left.Out;
        Vector2 p3 = right.P;
        Vector2 p2 = p3 + right.In;

        var p01 = p0.Lerp(p1, t);
        var p12 = p1.Lerp(p2, t);
        var p23 = p2.Lerp(p3, t);
        var p012 = p01.Lerp(p12, t);
        var p123 = p12.Lerp(p23, t);
        var p0123 = p012.Lerp(p123, t);

        // compute new handles
        var newLeftOut = p01 - p0;
        var newRightIn = p23 - p3;
        var newIn = p012 - p0123;
        var newOut = p123 - p0123;
        var newTri = new BezierPoint(p0123, newIn, newOut);

        return (newLeftOut, newTri, newRightIn);
    }

    /// <summary>
    /// Split the PolyCubicBezier at the given polyT into two new PolyCubicBeziers.
    /// </summary>
    [Pure] public static (BezierPoint[], BezierPoint[]) Split(this IReadOnlyList<BezierPoint> points, float polyT)
    {
        var (idx, t) = polyT.Modf();
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);

        var leftPoints = new BezierPoint[idx + 2];
        for (int i = 0; i < idx; i++)
            leftPoints[i] = points[i];
        leftPoints[idx] = points[idx].WithOut(leftOut);
        leftPoints[idx + 1] = tri;

        var rightPoints = new BezierPoint[points.Count - idx];
        rightPoints[0] = tri;
        rightPoints[1] = points[idx + 1].WithIn(rightIn);
        for (int i = idx + 2; i < points.Count; i++)
            rightPoints[i - idx] = points[i];

        return (leftPoints, rightPoints);
    }

    [Pure] public static BezierPoint[] Insert(this IReadOnlyList<BezierPoint> points, float polyT)
    {
        var (idx, t) = polyT.Modf();
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);

        var newPoints = new BezierPoint[points.Count + 1];
        for (int i = 0; i < idx; i++)
            newPoints[i] = points[i];
        newPoints[idx] = points[idx].WithOut(leftOut);
        newPoints[idx + 1] = tri;
        newPoints[idx + 2] = points[idx + 1].WithIn(rightIn);
        for (int i = idx + 2; i < points.Count; i++)
            newPoints[i + 1] = points[i];

        return newPoints;
    }

    public static void Insert(this List<BezierPoint> points, float polyT)
    {
        var (idx, t) = polyT.Modf();
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);
        points[idx] = points[idx].WithOut(leftOut);
        points[idx + 1] = points[idx + 1].WithIn(rightIn);
        points.Insert(idx + 1, tri);
    }

    // Sample curve segment at index, t in [0,1]
    [Pure] public static Vector2 Sample(this IReadOnlyList<BezierPoint> points, int index, float t)
    {
        var p0 = points[index].P;
        var p1 = p0 + points[index].Out;
        var p3 = points[index + 1].P;
        var p2 = p3 + points[index + 1].In;

        return p0.BezierInterpolate(p1, p2, p3, t);
    }

    // Sample the whole curve at a fractional t (e.g. 2.5 means between segment 2 and 3 at t=0.5)
    [Pure] public static Vector2 Sample(this IReadOnlyList<BezierPoint> points, float polyT)
    {
        if (points.Count < 2)
            throw new InvalidOperationException("Cannot sample from a non curve.");

        var (idx, t) = polyT.Modf();
        return points.Sample(idx, t);
    }

    [Pure] public static (Vector2[], float[]) Tessellate(this IReadOnlyList<BezierPoint> points, int subdivisionsPerSegment)
    {
        if (points.Count == 0) return ([], []);
        if (points.Count == 1) return ([points[0].P], [0f]);

        int total = (points.Count - 1) * subdivisionsPerSegment + 1;
        var polyline = new Vector2[total];
        var polyTs = new float[total];
        int k = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = 0; j < subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                polyline[k] = points.Sample(i, t);
                polyTs[k] = i + t;
                k++;
            }
        }
        // Add the last point
        polyline[k] = points[^1].P;
        polyTs[k] = points.Count - 1;
        return (polyline, polyTs);
    }

    public static (Vector2[] polyline, float[] ts) TessellateWithCache(
        this ImmutableArray<BezierPoint> points, int subdivisionsPerSegment = 64)
    {
        var key = (points, subdivisionsPerSegment);
        return TessCache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMilliseconds(500);
            return points.Tessellate(subdivisionsPerSegment);
        })!;
    }

    // --- X-monotone curve sampling (ImmutableArray, uses tessellation cache) ---

    public static float SampleX(this ImmutableArray<BezierPoint> points, float x)
    {
        var (polyline, _) = points.TessellateWithCache();
        return polyline.SampleX(x);
    }

    /// <summary>
    /// Sample the polyline by the given x ordered list from small to large.
    /// </summary>
    /// <param name="xs">The x values to sample at, must be in ascending order.</param>
    public static float[] SampleXList(this ImmutableArray<BezierPoint> points, IReadOnlyList<float> xs)
    {
        var (polyline, _) = points.TessellateWithCache();
        return polyline.SampleXList(xs);
    }

    public static Vector2 GetClosestPoint(this ImmutableArray<BezierPoint> points, Vector2 p, out float polyT)
    {
        var (polyline, cachedT) = points.TessellateWithCache();
        polyline.GetClosestPoint(p, out var rawPolyT);
        var (idx, lt) = rawPolyT.Modf();
        if (idx >= cachedT.Length - 1)
        {
            polyT = cachedT[^1];
            return polyline[^1];
        }
        var deltaT = lt * (cachedT[idx + 1] - cachedT[idx]);
        polyT = cachedT[idx] + deltaT;
        return ((IReadOnlyList<BezierPoint>)points).Sample(polyT);
    }

    // --- ImmutableArray<BezierPoint> mutation helpers (return new array) ---

    public static ImmutableArray<BezierPoint> WithPointPosition(this ImmutableArray<BezierPoint> points, int index, Vector2 position) => points.SetItem(index, points[index].WithPoint(position));

    public static ImmutableArray<BezierPoint> WithPointIn(this ImmutableArray<BezierPoint> points, int index, Vector2 inHandle) => points.SetItem(index, points[index].WithIn(inHandle));

    public static ImmutableArray<BezierPoint> WithPointInLinearly(this ImmutableArray<BezierPoint> points, int index, Vector2 inHandle)
    {
        var pt = points[index];
        var angleDelta = inHandle.Angle() - pt.In.Angle();
        var lengthDelta = inHandle.Length() - pt.In.Length();
        var newOut = pt.Out.Normalized().Rotated(angleDelta) * (pt.Out.Length() + lengthDelta);
        return points.SetItem(index, new BezierPoint(pt.P, inHandle, newOut));
    }

    public static ImmutableArray<BezierPoint> WithPointOut(this ImmutableArray<BezierPoint> points, int index, Vector2 outHandle) => points.SetItem(index, points[index].WithOut(outHandle));

    public static ImmutableArray<BezierPoint> WithPointOutLinearly(this ImmutableArray<BezierPoint> points, int index, Vector2 outHandle)
    {
        var pt = points[index];
        var angleDelta = outHandle.Angle() - pt.Out.Angle();
        var lengthDelta = outHandle.Length() - pt.Out.Length();
        var newIn = pt.In.Normalized().Rotated(angleDelta) * (pt.In.Length() + lengthDelta);
        return points.SetItem(index, new BezierPoint(pt.P, newIn, outHandle));
    }

    public static ImmutableArray<BezierPoint> WithPointAdded(
        this ImmutableArray<BezierPoint> points, Vector2 position, Vector2 inHandle, Vector2 outHandle, int at = -1)
    {
        var point = new BezierPoint(position, inHandle, outHandle);
        if (at >= 0 && at < points.Length)
            return points.Insert(at, point);
        return points.Add(point);
    }

    public static ImmutableArray<BezierPoint> WithPointRemoved(this ImmutableArray<BezierPoint> points, int index) => points.RemoveAt(index);

    /// <summary>
    /// Insert a bezier control point at a fractional polyT without changing the visual shape.
    /// </summary>
    /// <returns>(new array, index of inserted point) or (original array, -1) if invalid.</returns>
    public static (ImmutableArray<BezierPoint> result, int insertedIndex) TryInsertPoint(
        this ImmutableArray<BezierPoint> points, float polyT)
    {
        if (points.Length < 2 || polyT <= 0f || polyT >= points.Length - 1)
            return (points, -1);
        var (idx, t) = polyT.Modf();
        if (t <= 0f || t >= 1f) return (points, -1);
        var newList = points.Insert(polyT);
        return (ImmutableCollectionsMarshal.AsImmutableArray(newList), idx + 1);
    }

    public static float GetPointInTangent(this ImmutableArray<BezierPoint> points, int index)
    {
        var pt = points[index];
        return pt.In.Y / pt.In.X;
    }

    public static float GetPointOutTangent(this ImmutableArray<BezierPoint> points, int index)
    {
        var pt = points[index];
        return pt.Out.Y / pt.Out.X;
    }

    public static ImmutableArray<BezierPoint> WithPointInTangent(this ImmutableArray<BezierPoint> points, int index, float tangent)
    {
        var pt = points[index];
        var oldHandle = pt.In;
        float length = oldHandle.Length();
        if (length <= 0f) return points;
        float sign = oldHandle.X != 0f ? MathF.Sign(oldHandle.X) : MathF.Sign(oldHandle.Y);
        var baseDir = new Vector2(1f, tangent).Normalized();
        return points.SetItem(index, pt.WithIn(baseDir * sign * length));
    }

    public static ImmutableArray<BezierPoint> WithPointOutTangent(this ImmutableArray<BezierPoint> points, int index, float tangent)
    {
        var pt = points[index];
        var oldHandle = pt.Out;
        float length = oldHandle.Length();
        if (length <= 0f) return points;
        float sign = oldHandle.X != 0f ? MathF.Sign(oldHandle.X) : MathF.Sign(oldHandle.Y);
        var baseDir = new Vector2(1f, tangent).Normalized();
        return points.SetItem(index, pt.WithOut(baseDir * sign * length));
    }
}