using System;
using Godot;

namespace Ciallo.Rendering;

public partial class BodyHolder : Node2D
{
    public void SetAreaCursor(Control.CursorShape shape)
    {
        foreach (var child in GetChildren())
        {
            switch (child)
            {
                case Body body:
                    body.MouseDefaultCursorShape = shape;
                    break;
                case BodyHolder bodyHolder:
                    bodyHolder.SetAreaCursor(shape);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected child of BodyHolder: {child}");
            }
        }
    }
}