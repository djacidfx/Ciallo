using System.Collections.Generic;
using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public partial class CursorDetectionArea : StaticBody2D
{
    private Control.CursorShape _mouseDefaultCursorShape = Control.CursorShape.Arrow;
    private List<Rid> _shapes = [];

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

    public void AddShapeRid(Rid id)
    {
        _shapes.Add(id);
        PhysicsServer2D.BodyAddShape(GetRid(), id);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            PhysicsServer2D.BodyClearShapes(GetRid());
            _shapes.ForEach(PhysicsServer2D.FreeRid);
            _shapes.Clear();
        }
    }

    // Note: If there is a button overlay on world, _MouseEntered won't work.
}