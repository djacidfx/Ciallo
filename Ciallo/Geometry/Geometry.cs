using Godot;

namespace Ciallo.Geometry;

// Hold scattered geometry/math functions
public static partial class Geometry
{
    // Centripetal Catmull–Rom spline interpolation
    public static Vector2 CatmullRomInterpolation(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        bool isLinear = p1.IsEqualApprox(p2) || (p0.IsEqualApprox(p1) && p2.IsEqualApprox(p3));
        if (isLinear) return p1.Lerp(p2, t);

        // Ensure p0, p1, p2, p3 form an isosceles trapezoid when one endpoint coincides with its neighbor.
        // Bases: p12 and p03; legs: p01 and p23.
        var tangent = (p2 - p1).Normalized();
        var normal = new Vector2(-tangent.Y, tangent.X);

        if (p0.IsEqualApprox(p1) && !p2.IsEqualApprox(p3))
        {
            // Compute p0 so that p03 is parallel to p12 and |p01| == |p23|
            var d = p3 - p2;
            var s = -d.Dot(tangent);
            var h = d.Dot(normal);
            p0 = p1 + h * normal + s * tangent;
        }
        else if (p2.IsEqualApprox(p3) && !p0.IsEqualApprox(p1))
        {
            var d = p0 - p1;
            var s = d.Dot(tangent);
            var h = d.Dot(normal);
            p3 = p2 + h * normal - s * tangent;
        }

        const float alpha = 0.5f;
        var t0 = 0f;
        var t1 = t0 + Mathf.Pow(p0.DistanceTo(p1), alpha);
        var t2 = t1 + Mathf.Pow(p1.DistanceTo(p2), alpha);
        var t3 = t2 + Mathf.Pow(p2.DistanceTo(p3), alpha);

        var tt = t1 + (t2 - t1) * t;

        var a1 = ((t1 - tt) / (t1 - t0)) * p0 + ((tt - t0) / (t1 - t0)) * p1;
        var a2 = ((t2 - tt) / (t2 - t1)) * p1 + ((tt - t1) / (t2 - t1)) * p2;
        var a3 = ((t3 - tt) / (t3 - t2)) * p2 + ((tt - t2) / (t3 - t2)) * p3;

        var b1 = ((t2 - tt) / (t2 - t0)) * a1 + ((tt - t0) / (t2 - t0)) * a2;
        var b2 = ((t3 - tt) / (t3 - t1)) * a2 + ((tt - t1) / (t3 - t1)) * a3;

        var c = ((t2 - tt) / (t2 - t1)) * b1 + ((tt - t1) / (t2 - t1)) * b2;
        if (float.IsNaN(c.X) || float.IsNaN(c.Y))
        {
            GD.PrintErr($"CatmullRomInterpolation produced NaN result. Inputs: p0={p0}, p1={p1}, p2={p2}, p3={p3}, t={t}");
            c = p1.Lerp(p2, t);
        }
        return c;
    }

    // Centripetal Catmull–Rom spline interpolation (scalar overload)
    public static float CatmullRomInterpolation(float p0, float p1, float p2, float p3, float t)
    {
        bool isLinear = Mathf.IsEqualApprox(p1, p2) || (Mathf.IsEqualApprox(p0, p1) && Mathf.IsEqualApprox(p2, p3));
        if (isLinear) return Mathf.Lerp(p1, p2, t);

        // Assume acceleration is constant
        if (Mathf.IsEqualApprox(p0, p1)) p0 = p1 - (p3 - p2);
        if (Mathf.IsEqualApprox(p2, p3)) p3 = p2 + (p1 - p0);

        const float alpha = 0.5f;
        var t0 = 0f;
        var t1 = t0 + Mathf.Pow(Mathf.Abs(p0 - p1), alpha);
        var t2 = t1 + Mathf.Pow(Mathf.Abs(p1 - p2), alpha);
        var t3 = t2 + Mathf.Pow(Mathf.Abs(p2 - p3), alpha);

        var tt = t1 + (t2 - t1) * t;

        var a1 = ((t1 - tt) / (t1 - t0)) * p0 + ((tt - t0) / (t1 - t0)) * p1;
        var a2 = ((t2 - tt) / (t2 - t1)) * p1 + ((tt - t1) / (t2 - t1)) * p2;
        var a3 = ((t3 - tt) / (t3 - t2)) * p2 + ((tt - t2) / (t3 - t2)) * p3;

        var b1 = ((t2 - tt) / (t2 - t0)) * a1 + ((tt - t0) / (t2 - t0)) * a2;
        var b2 = ((t3 - tt) / (t3 - t1)) * a2 + ((tt - t1) / (t3 - t1)) * a3;

        var c = ((t2 - tt) / (t2 - t1)) * b1 + ((tt - t1) / (t2 - t1)) * b2;
        if (float.IsNaN(c))
        {
            // Note: hope this is the only source where nan possible in our program, so no guard in other systems.
            GD.PrintErr($"CatmullRomInterpolation produced NaN result. Inputs: p0={p0}, p1={p1}, p2={p2}, p3={p3}, t={t}");
            c = float.Lerp(p1, p2, t);
        }
        return c;
    }

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