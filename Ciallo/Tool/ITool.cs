using Ciallo.Geometry;
using Ciallo.Widget;
using Frent;
using Godot;

namespace Ciallo.Tool;

public interface ITool
{
    public void OnLeftClick(CursorButtonData data);
    public void OnLeftRelease(CursorButtonData data);
    public void OnRightClick(CursorButtonData data);
    public void OnRightRelease(CursorButtonData data);
    public void OnMoving(CursorMotionData data);
    public void OnKey(InputEventKey key);

    public void DrawProperty(PropertyContainer container);
    public bool CanHandleLayer(params Entity[] layerEs);
    public void OnActivate(params Entity[] layerEs);
    public void OnDeactivate();
}