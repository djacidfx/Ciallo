using Godot;

namespace Ciallo.Geometry;

// Hold scattered geometry/math functions
public static partial class Geometry
{
    public static float DistanceToLine(this Vector2 p, Vector2 linePoint, Vector2 lineDir)
    {
        if (lineDir.IsZeroApprox()) return p.DistanceTo(linePoint);

        // The projection of vector `ap` (from linePoint to p) onto `lineDir` gives the
        // closest point on the line. The parameter 't' is the scale factor for `lineDir`.
        var ap = p - linePoint;
        var t = ap.Dot(lineDir.Normalized());
        var closestPoint = linePoint + lineDir * t;

        return p.DistanceTo(closestPoint);
    }

    public static float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
    {
        // 2 * area of triangle via cross product magnitude
        var ab = b - a;
        var ac = c - a;
        float cross = ab.X * ac.Y - ab.Y * ac.X;
        return Mathf.Abs(cross) * 0.5f;
    }

    /// <remarks>
    /// Return null if p0 == p1 or p2 == p3 or regular no intersection.
    /// </remarks>
    public static Vector2? SegmentIntersect(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var s1 = p1 - p0;
        var s2 = p3 - p2;

        var s = (-s1.Y * (p0.X - p2.X) + s1.X * (p0.Y - p2.Y)) / (-s2.X * s1.Y + s1.X * s2.Y);
        var t = (s2.X * (p0.Y - p2.Y) - s2.Y * (p0.X - p2.X)) / (-s2.X * s1.Y + s1.X * s2.Y);

        if (s is >= 0 and <= 1 && t is >= 0 and <= 1)
        {
            return p0 + (t * s1);
        }

        return null; // No collision
    }
}