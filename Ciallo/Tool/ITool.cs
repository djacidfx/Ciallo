using Arch.Core;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

public interface ITool
{
    public void OnLeftClick(CursorButtonData data);
    public void OnLeftRelease(CursorButtonData data);
    public void OnRightClick(CursorButtonData data);
    public void OnRightRelease(CursorButtonData data);
    public void OnMoving(CursorMotionData data);

    public void OnRegisterDocument(Entity document)
    {
    }

    void OnKey(InputEventKey key);
}