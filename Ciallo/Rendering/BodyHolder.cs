using System;
using Godot;

namespace Ciallo.Rendering;

public partial class BodyHolder : Node2D
{
    public void SetChildrenBodyCursor(Control.CursorShape shape)
    {
        foreach (var child in GetChildren())
        {
            switch (child)
            {
                case Body body:
                    body.MouseDefaultCursorShape = shape;
                    break;
                case BodyHolder bodyHolder:
                    bodyHolder.SetChildrenBodyCursor(shape);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected child of BodyHolder: {child}");
            }
        }
    }
}