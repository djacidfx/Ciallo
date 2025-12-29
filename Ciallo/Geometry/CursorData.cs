using System;
using System.Diagnostics;
using Godot;

namespace Ciallo.Geometry;

[DebuggerDisplay("{ToString(),nq}")]
public struct CursorButtonData
{
    public Vector2 ScreenPosition;
    public Vector2 WorldPosition;
    public float Pressure;
    public Vector2 Tilt;

    public static explicit operator CursorMotionData(CursorButtonData b) =>
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

    public override string ToString() => $"CursorButtonData(Screen={ScreenPosition}, World={WorldPosition}, Pressure={Pressure:F3}, Tilt={Tilt})";
}

[DebuggerDisplay("{ToString(),nq}")]
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

    public TimeSpan TimeDelta;
    public float TimeDeltaMs => (float)TimeDelta.TotalMilliseconds;
    public float TimeDeltaSec => (float)TimeDelta.TotalSeconds;

    public Vector2 PrevWorldPosition => WorldPosition - WorldDelta;
    public Vector2 PrevScreenPosition => ScreenPosition - ScreenDelta;
    public float PrevPressure => Pressure - PressureDelta;
    public Vector2 PrevTilt => Tilt - TiltDelta;
    public float ScreenSpeed => ScreenDelta.Length() / TimeDeltaMs;
    public float WorldSpeed => WorldDelta.Length() / TimeDeltaMs;
    public Vector2 WorldDirection => WorldDelta.Normalized();
    public Vector2 ScreenDirection => ScreenDelta.Normalized();

    public static implicit operator CursorButtonData(CursorMotionData m) =>
        new()
        {
            ScreenPosition = m.ScreenPosition,
            WorldPosition = m.WorldPosition,
            Pressure = m.Pressure,
            Tilt = m.Tilt
        };

    public override string ToString() =>
        $"CursorMotionData(Screen={ScreenPosition}, ScreenDelta={ScreenDelta}, World={WorldPosition}, WorldDelta={WorldDelta}, " +
        $"Pressure={Pressure:F3} (Δ={PressureDelta:F3}), Tilt={Tilt} (Δ={TiltDelta}), TimeMs={TimeDeltaMs:F1}, WorldSpeed={WorldSpeed:F3})";
}