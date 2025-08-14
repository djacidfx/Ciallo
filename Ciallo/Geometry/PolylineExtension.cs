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
}