using System;
using Godot;

namespace Ciallo.Rendering;

public partial class WorldButtonManager : Node2D
{
    private CanvasLayer _canvasLayer;
    private Button _cursorSwitcher;

    public Control.CursorShape DefaultCursorShape
    {
        get => _cursorSwitcher.MouseDefaultCursorShape;
        set => _cursorSwitcher.MouseDefaultCursorShape = value;
    }

    public override void _EnterTree()
    {
        _canvasLayer = GetChild<CanvasLayer>(0);
        _cursorSwitcher = _canvasLayer.GetChild<Button>(0);
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