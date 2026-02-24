using Godot;

namespace Ciallo;

public static class Rect2Extension
{
    public static Vector2[] GetCorners(this Rect2 rect) =>
    [
        rect.Position,
        rect.Position + new Vector2(rect.Size.X, 0),
        rect.Position + rect.Size,
        rect.Position + new Vector2(0, rect.Size.Y),
    ];
}