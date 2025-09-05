using Godot;

namespace Ciallo.NodeControl;

public struct CursorButtonData
{
    public Vector2 ScreenPosition;
    public Vector2 WorldPosition;
    
    public InputEventMouseButton RawData;
}

public struct CursorMotionData
{
    public Vector2 ScreenPosition;
    public Vector2 ScreenDelta;
    public Vector2 WorldPosition;
    public Vector2 WorldDelta;
    public float Pressure;
    public float PressureDelta;

    public Vector2 Tilt;
    public Vector2 TiltDelta;
    
    public InputEventMouseMotion RawData;
}