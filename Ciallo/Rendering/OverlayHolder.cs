using System;
using Godot;

namespace Ciallo.Rendering;

public partial class OverlayHolder : Node2D
{
    public void SetVisibility(bool visible)
    {
        foreach (var child in GetChildren())
        {
            switch (child)
            {
                case OverlayHolder overlayHolder:
                    overlayHolder.SetVisibility(visible);
                    break;
                case Node2D node2D:
                    node2D.Visible = visible;
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected child of OverlayHolder: {child}");
            }
        }
    }
}