using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public partial class CursorDetectionArea : StaticBody2D
{
    private Control.CursorShape _mouseDefaultCursorShape = Control.CursorShape.Arrow;

    public bool IsHovered { get; set; } // Should only be set by manager

    public Control.CursorShape MouseDefaultCursorShape
    {
        get => _mouseDefaultCursorShape;
        set
        {
            _mouseDefaultCursorShape = value;
            GetViewport()?.UpdateMouseCursorState();
        }
    }

    public CursorDetectionArea()
    {
        CollisionLayer = AppGodotLayers.Physics2D.Stroke;
        CollisionMask = AppGodotLayers.Physics2D.Empty; // Only detect mouse input, don't collide with anything else
        InputPickable = true;
    }
    // Note: There is a button overlay on world, _MouseEntered won't work. 
}