using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// "Tool" is a concept/object to handle user input and modify the canvas.
/// `ITool` here defines those user inputs a tool should handle.
/// Developer can directly implement this interface to create a tool processing raw input.
/// Or derive from the `ToolBase` with "interactive session" abstraction to create common tools more easily.
/// </summary>
public interface ITool
{
    public void OnMouseButton(InputEventMouseButton button, CursorButtonData data);
    // Return true if the event is handled
    public bool OnKey(InputEventKey key);

    public void OnMoving(CursorMotionData data);

    public void DrawProperty(PropertyContainer container);
    public bool CanHandleLayer(params Entity[] layerEs);
    public void OnActivate(params Entity[] layerEs);
    public void OnDeactivate();
}