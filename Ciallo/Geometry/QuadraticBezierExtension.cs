using System.Diagnostics.Contracts;
using Godot;

namespace Ciallo.Geometry;

public static class QuadraticBezierExtension
{
    /// <summary>
    /// Identical to Vector2.BezierInterpolate but for quadratic Bezier curves.
    /// </summary>
    [Pure] public static Vector2 QuadraticBezierInterpolate(this Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
    }

    [Pure] public static Vector2 QuadraticBezierDerivative(this Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return 2f * (u * (p1 - p0) + t * (p2 - p1));
    }
}