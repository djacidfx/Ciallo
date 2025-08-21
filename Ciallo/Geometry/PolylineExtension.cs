using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ciallo.Geometry;

public static class PolylineExtension
{
    public static Rect2 GetBoundingBox([NotNull] this IEnumerable<Vector2> polyline, IEnumerable<float> radii = null)
    {
        var radiiSource = radii ?? Enumerable.Repeat(0f, int.MaxValue);
        using var pEnum = polyline.GetEnumerator();
        using var rEnum = radiiSource.GetEnumerator();
        if (!pEnum.MoveNext() || !rEnum.MoveNext())
            return default;
        

        Vector2 first = pEnum.Current;
        float firstR = rEnum.Current;
        float minX = first.X - firstR;
        float minY = first.Y - firstR;
        float maxX = first.X + firstR;
        float maxY = first.Y + firstR;

        while (pEnum.MoveNext() && rEnum.MoveNext())
        {
            Vector2 p = pEnum.Current;
            float r = rEnum.Current;
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
    public static Vector2 GetClosestPoint([NotNull] this IEnumerable<Vector2> polyline, Vector2 point, out float t)
    {
        t = 0f;
        using var enumerator = polyline.GetEnumerator();

        if (!enumerator.MoveNext())
            return Vector2.Zero;

        var p1 = enumerator.Current;

        if (!enumerator.MoveNext())
            return p1;

        var closestPoint = p1;
        var minDistanceSq = point.DistanceSquaredTo(p1);
        var segmentIndex = 0;

        do
        {
            var p2 = enumerator.Current;
            var segment = p2 - p1;
            var segmentLengthSq = segment.LengthSquared();

            Vector2 currentClosest;
            float currentT;

            if (segmentLengthSq < 1e-7f)
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
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                closestPoint = currentClosest;
                t = segmentIndex + currentT;
            }

            p1 = p2;
            segmentIndex++;
        } while (enumerator.MoveNext());

        return closestPoint;
    }
}