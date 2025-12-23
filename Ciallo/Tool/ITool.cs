using System;
using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;

namespace Ciallo.Tool;

public interface ITool
{
    // Return true if the event triggers interaction
    // During interaction:
    //  - mouse is captured by canvas
    //  - all the key inputs are sent to the tool (other operations' shortcut like undo/redo won't be triggered)
    // Return false to quit the interaction
    public bool OnLeftClick(CursorButtonData data);
    public bool OnLeftRelease(CursorButtonData data);
    public bool OnRightClick(CursorButtonData data);
    public bool OnRightRelease(CursorButtonData data);
    public ToolKeyActions OnKey(InputEventKey key);

    public void OnMoving(CursorMotionData data);

    public void DrawProperty(PropertyContainer container);
    public bool CanHandleLayer(params Entity[] layerEs);
    public void OnActivate(params Entity[] layerEs);
    public void OnDeactivate();
}

[Flags]
public enum ToolKeyActions
{
    None = 0,
    HandleInput = 1 << 0,
    Interact = 1 << 1,
}