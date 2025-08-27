using Godot;

namespace Ciallo.View;

public struct CursorMotionData
{
    public Vector2 ScreenPosition;
    public Vector2 ScreenDelta;
    public Vector2 WorldPosition;
    public Vector2 WorldDelta;
    
    public InputEventMouseMotion RawData;
}