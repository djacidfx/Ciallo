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
    public void OnKey(InputEventKey key);
    
    public void OnActivate(){}
    public void OnDeactivate(){}
}