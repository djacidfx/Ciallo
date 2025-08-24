using System;
using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace Ciallo.Geometry;

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
            t = i-1 + currentT;
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
        float SampleSegment(Vector2 p0, Vector2 p1, float xValue)
        {
            float slope = (p1.Y - p0.Y) / (p1.X - p0.X);
            return p0.Y + slope * (xValue - p0.X);
        }

        if (polyline.Count == 2)
        {
            return SampleSegment(polyline[0], polyline[1], x);
        }

        var searchResult = Array.BinarySearch(polyline.Select(v=>v.X).ToArray(), x);
        if (searchResult >= 0) return polyline[searchResult].Y;
        // Get the index of the closest point after x
        // see https://learn.microsoft.com/en-us/dotnet/api/system.array.binarysearch for the return value.
        int idx = ~searchResult;
        if(idx == 0) return polyline[0].Y;
        if(idx == polyline.Count) return polyline[^1].Y;
        return SampleSegment(polyline[idx-1], polyline[idx], x);
    }
}