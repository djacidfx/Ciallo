using Godot;

namespace Ciallo.Geometry;

// Hold scattered geometry/math functions
public static partial class Geometry
{
    // Centripetal Catmull–Rom spline interpolation
    public static Vector2 CatmullRomInterpolation(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
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
        return c;
    }
}