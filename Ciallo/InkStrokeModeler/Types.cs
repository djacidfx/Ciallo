using System;
using System.Numerics;

namespace InkStrokeModeler;

public static class Vector2Extensions
{
    public static bool IsFinite(this Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    public static float AbsoluteAngleTo(this Vector2 value, Vector2 other)
    {
        if (!value.IsFinite() || !other.IsFinite())
            throw new ArgumentException($"Non-finite inputs: value={value}; other={other}.");

        float magnitude = value.Length();
        float otherMagnitude = other.Length();
        if (magnitude == 0 || otherMagnitude == 0) return 0;

        Vector2 unit = value / magnitude;
        Vector2 otherUnit = other / otherMagnitude;
        return MathF.Acos(Math.Clamp(Vector2.Dot(unit, otherUnit), -1f, 1f));
    }
}

public enum InputEventType
{
    Down,
    Move,
    Up,
}

public readonly record struct ModelerInput(
    InputEventType EventType,
    Vector2 Position,
    TimeSpan Time,
    float Pressure = -1,
    float Tilt = -1,
    float Orientation = -1);

public readonly record struct ModelerResult(
    Vector2 Position,
    Vector2 Velocity,
    Vector2 Acceleration,
    TimeSpan Time,
    float Pressure = -1,
    float Tilt = -1,
    float Orientation = -1);

public static class ModelerInputValidation
{
    public static void Validate(ModelerInput input)
    {
        if (!input.Position.IsFinite()) throw new ArgumentException("Input position must be finite.");
    }
}
