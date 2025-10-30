using Godot;

namespace Ciallo.Geometry;

public struct CursorButtonData
{
    public Vector2 ScreenPosition;
    public Vector2 WorldPosition;
    public float Pressure;
    public Vector2 Tilt;

    public static implicit operator CursorMotionData(CursorButtonData b) =>
        new()
        {
            ScreenPosition = b.ScreenPosition,
            WorldPosition = b.WorldPosition,
            Pressure = b.Pressure,
            Tilt = b.Tilt,

            ScreenDelta = Vector2.Zero,
            WorldDelta = Vector2.Zero,
            PressureDelta = 0f,
            TiltDelta = Vector2.Zero,
        };
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

    public double TimeDeltaMs;

    public Vector2 PrevWorldPosition => WorldPosition - WorldDelta;
    public Vector2 PrevScreenPosition => ScreenPosition - ScreenDelta;
    public float PrevPressure => Pressure - PressureDelta;
    public Vector2 PrevTilt => Tilt - TiltDelta;

    public static implicit operator CursorButtonData(CursorMotionData m) =>
        new()
        {
            ScreenPosition = m.ScreenPosition,
            WorldPosition = m.WorldPosition,
            Pressure = m.Pressure,
            Tilt = m.Tilt
        };
}