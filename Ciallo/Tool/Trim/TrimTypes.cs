using Frent;
using Godot;

namespace Ciallo.Tool;

public sealed record TrimHighlightSegment(Vector2[] Points, float Radius);

public readonly record struct TrimEdgeRange(float FromT, float ToT);

public record struct TrimRangeBoundary(float FromT, float ToT, Vector2 FromPoint, Vector2 ToPoint)
{
    public bool HasFromPoint;
    public bool HasToPoint;
}

public readonly record struct TrimTargetSplit(Entity Shape, float PolyT);

public readonly record struct TrimSnapResult(Entity Shape, float PolyT, Vector2 Point);
