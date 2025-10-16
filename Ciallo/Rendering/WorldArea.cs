using System;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Rendering;

public partial class WorldArea : Node2D
{
    private CanvasLayer _canvasLayer;
    private Control _cursorSwitcher; // This is supposed to be the job of ViewportContainer, but it doesn't reponse even if changing MouseDefaultCursorShape.

    public Control.CursorShape DefaultCursorShape { get; set; }
    private CursorDetectArea _hoveringArea;
    public CursorDetectArea HoveringArea
    {
        get => _hoveringArea;
        private set
        {
            if (_hoveringArea == value) return;

            if (HoveringArea != null) HoveringArea.IsHovered = false;
            if (value != null) value.IsHovered = true;
            _hoveringArea = value;
            _cursorSwitcher.MouseDefaultCursorShape = value?.CursorShape ?? DefaultCursorShape;
        }
    }

    public override void _EnterTree()
    {
        _canvasLayer = GetChild<CanvasLayer>(0);
        _cursorSwitcher = _canvasLayer.GetChild<Control>(0);
    }

    // Note: not implement screen position, world size
    public Button AddRectButton(Vector2 position, float size, WorldButtonFlags flags = default)
    {
        return AddRectButton(position, new Vector2(size, size), flags);
    }

    public Button AddRectButton(Vector2 position, Vector2 size, WorldButtonFlags flags = default)
    {
        var button = AddRectButton(flags);
        button.Position = flags.HasFlag(WorldButtonFlags.CornerPosition) ? position : position - size * 0.5f;
        button.Size = size;
        button.PivotOffset = flags.HasFlag(WorldButtonFlags.CornerPosition) ? Vector2.Zero : size * 0.5f;

        return button;
    }

    public Button AddRectButton(WorldButtonFlags flags = default)
    {
        var button = new Button();

        if (flags.HasFlag(WorldButtonFlags.ScreenPosition))
            _canvasLayer.AddChild(button);
        else
            AddChild(button);

        button.Flat = true;

        return button;
    }


    public void OnCursorMove(CursorMotionData data)
    {
        // See following pages for the point query method:
        // https://docs.godotengine.org/en/stable/tutorials/physics/ray-casting.html
        // https://godotforums.org/d/34175-collision-with-point
        // Note this is different to RayCast2D node, which is a ray on XY plane. We want a top-down cast here (a point on XY plane).
        var pp = new PhysicsPointQueryParameters2D()
        {
            CollideWithBodies = true,
            Position = data.WorldPosition,
            CollisionMask = (uint)AppGodotLayers.Physics2DLayerMask.Stroke
        };
        var points = GetWorld2D().DirectSpaceState.IntersectPoint(pp, 1);
        if (points.Count > 0)
        {
            var area = (CursorDetectArea)points[0]["collider"];
            HoveringArea = area;
        }
        else
        {
            HoveringArea = null;
        }
    }
}

[Flags]
public enum WorldButtonFlags
{
    None = 0,

    /// <summary>Interpret the position as screen coordinates.</summary>
    ScreenPosition = 1 << 0,

    /// <summary>Interpret the size as a screen pixel measurement.</summary>
    ScreenSize = 1 << 1,

    /// <summary>Given position is the upper left corner of a button.</summary>
    CornerPosition = 1 << 2
}