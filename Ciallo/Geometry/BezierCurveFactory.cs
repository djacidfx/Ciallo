using System.Collections.Immutable;
using Godot;

namespace Ciallo.Geometry;

public static class BezierCurveFactory
{
    private const float L = 0.4f;

    public static ImmutableArray<BezierPoint> Constant(float y = 0.0f) =>
    [
        new(new(0f, y), new(-L, 0f), new(L, 0f)),
        new(new(1f, y), new(-L, 0f), new(L, 0f))
    ];

    public static ImmutableArray<BezierPoint> Linear(float y0 = 0.0f, float y1 = 1.0f)
    {
        var v = new Vector2(1f, y1 - y0);
        var len = v.Length();
        var dl = v / len * L;
        return
        [
            new(new(0f, y0), -dl, dl),
            new(new(1f, y1), -dl, dl)
        ];
    }

    public static ImmutableArray<BezierPoint> EaseInOut(float y0 = 0.0f, float y1 = 1.0f) =>
    [
        new(new(0f, y0), new(-L, 0f), new(L, 0f)),
        new(new(1f, y1), new(-L, 0f), new(L, 0f))
    ];

    public static ImmutableArray<BezierPoint> EaseInOut(Vector2 p0, Vector2 p1) =>
    [
        new(p0, new(-L, 0f), new(L, 0f)),
        new(p1, new(-L, 0f), new(L, 0f))
    ];

    public static ImmutableArray<BezierPoint> GPenCurve(float y0 = 0.0f, float y1 = 1.0f)
    {
        var dy = y1 - y0;
        // Ease-in like x², but the tangent at (0, y0) has a slight upward slope
        // instead of being fully horizontal, giving a G-pen "bite" feel.
        var startTangent = new Vector2(L, dy * 0.3f).Normalized() * L;
        return
        [
            new(new(0f, y0), -startTangent, startTangent),
            new(new(1f, y1), new(-L, 0f), new(L, 0f))
        ];
    }
}