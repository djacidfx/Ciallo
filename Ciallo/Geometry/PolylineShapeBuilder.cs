using System.Collections.Immutable;
using Godot;

namespace Ciallo.Geometry;

public static class PolylineShapeBuilder
{
    private const int OctagonSides = 8;

    public static ImmutableArray<Vector2> BuildClosedOctagon(Vector2 center, float radius)
    {
        var builder = ImmutableArray.CreateBuilder<Vector2>(OctagonSides + 1);
        for (int i = 0; i < OctagonSides; i++)
        {
            float angle = Mathf.Tau * i / OctagonSides;
            builder.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        builder.Add(builder[0]);
        return builder.ToImmutable();
    }
}
