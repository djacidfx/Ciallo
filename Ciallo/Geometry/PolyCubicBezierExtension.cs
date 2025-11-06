using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// Hold all the pure geometry/math calculations for PolyCubicBezier.
/// </summary>
public static class PolyCubicBezierExtension
{
    [Pure] public static Rect2 GetBoundingBox(this IReadOnlyList<BezierCurve.Point> points)
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

    [Pure] public static bool IsXMonotone(this IReadOnlyList<BezierCurve.Point> points)
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

    [Pure] public static (Vector2, BezierCurve.Point, Vector2) CalculateInsertPoint(
        this IReadOnlyList<BezierCurve.Point> points, int idx, float t)
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
        var newTri = new BezierCurve.Point(p0123, newIn, newOut);

        return (newLeftOut, newTri, newRightIn);
    }

    /// <summary>
    /// Split the PolyCubicBezier at the given polyT into two new PolyCubicBeziers.
    /// </summary>
    [Pure] public static (List<BezierCurve.Point>, List<BezierCurve.Point>) Split(this IReadOnlyList<BezierCurve.Point> points, float polyT)
    {
        var (idx, t) = ResolvePolyT(polyT);
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);

        var leftPoints = new List<BezierCurve.Point>();
        for (int i = 0; i < idx; i++)
            leftPoints.Add(points[i]);
        leftPoints.Add(points[idx].WithOut(leftOut));
        leftPoints.Add(tri);

        // ReSharper disable once UseObjectOrCollectionInitializer
        var rightPoints = new List<BezierCurve.Point>();
        rightPoints.Add(tri);
        rightPoints.Add(points[idx + 1].WithIn(rightIn));
        for (int i = idx + 2; i < points.Count; i++)
            rightPoints.Add(points[i]);

        return (leftPoints, rightPoints);
    }

    [Pure] public static List<BezierCurve.Point> Insert(this IReadOnlyList<BezierCurve.Point> points, float polyT)
    {
        var (idx, t) = ResolvePolyT(polyT);
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);

        var newPoints = new List<BezierCurve.Point>();
        for (int i = 0; i < idx; i++)
            newPoints.Add(points[i]);
        newPoints.Add(points[idx].WithOut(leftOut));
        newPoints.Add(tri);
        newPoints.Add(points[idx + 1].WithIn(rightIn));
        for (int i = idx + 2; i < points.Count; i++)
            newPoints.Add(points[i]);

        return newPoints;
    }

    public static void Insert(this List<BezierCurve.Point> points, float polyT)
    {
        var (idx, t) = ResolvePolyT(polyT);
        var (leftOut, tri, rightIn) = points.CalculateInsertPoint(idx, t);
        points[idx] = points[idx].WithOut(leftOut);
        points[idx + 1] = points[idx + 1].WithIn(rightIn);
        points.Insert(idx + 1, tri);
    }

    [Pure] public static (int idx, float t) ResolvePolyT(this float polyT)
    {
        int idx = (int)Math.Floor(polyT);
        float t = polyT - idx;
        return (idx, t);
    }

    // Sample curve segment at index, t in [0,1]
    [Pure] public static Vector2 Sample(this IReadOnlyList<BezierCurve.Point> points, int index, float t)
    {
        var p0 = points[index].P;
        var p1 = p0 + points[index].Out;
        var p3 = points[index + 1].P;
        var p2 = p3 + points[index + 1].In;

        return p0.BezierInterpolate(p1, p2, p3, t);
    }

    // Sample the whole curve at a fractional t (e.g. 2.5 means between segment 2 and 3 at t=0.5)
    [Pure] public static Vector2 Sample(this IReadOnlyList<BezierCurve.Point> points, float polyT)
    {
        if (points.Count < 2)
            throw new InvalidOperationException("Cannot sample from a non curve.");

        var (idx, t) = ResolvePolyT(polyT);
        return points.Sample(idx, t);
    }

    [Pure] public static (List<Vector2>, List<float>) Tessellate(this IReadOnlyList<BezierCurve.Point> points, int subdivisionsPerSegment)
    {
        if (points.Count == 0) return ([], []);
        if (points.Count == 1) return ([points[0].P], [0f]);

        List<Vector2> polyline = [];
        List<float> polyTs = [];
        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = 0; j < subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                polyline.Add(points.Sample(i, t));
                polyTs.Add(i + t);
            }
        }
        // Add the last point
        polyline.Add(points[^1].P);
        polyTs.Add(points.Count - 1);
        return (polyline, polyTs);
    }
}