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
}