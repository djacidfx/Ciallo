using System;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal static class Utils
{
    public const float Pi = MathF.PI;

    public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    public static float Normalize01(float start, float end, float value)
    {
        if (start == end) return value > start ? 1 : 0;
        return Clamp01((value - start) / (end - start));
    }

    public static float InverseLerp(float a, float b, float value)
    {
        if (b - a == 0f) return 0f;
        return (value - a) / (b - a);
    }

    public static float Interp(float start, float end, float amount) =>
        start + (end - start) * Clamp01(amount);

    public static double Interp(double start, double end, float amount) =>
        start + (end - start) * Clamp01(amount);

    public static Vector2 Interp(Vector2 start, Vector2 end, float amount) =>
        start + (end - start) * Clamp01(amount);

    public static TimeSpan Interp(TimeSpan start, TimeSpan end, float amount) =>
        start + (end - start) * Clamp01(amount);

    public static float InterpAngle(float start, float end, float amount)
    {
        start = NormalizeAngle(start);
        end = NormalizeAngle(end);
        float delta = end - start;
        if (delta < -Pi) end += 2 * Pi;
        else if (delta > Pi) end -= 2 * Pi;
        return NormalizeAngle(Interp(start, end, amount));
    }

    public static ModelerResult InterpResult(ModelerResult start, ModelerResult end, float amount) =>
        new(
            Interp(start.Position, end.Position, amount),
            Interp(start.Velocity, end.Velocity, amount),
            Interp(start.Acceleration, end.Acceleration, amount),
            Interp(start.Time, end.Time, amount),
            start.Pressure < 0 || end.Pressure < 0 ? -1 : Interp(start.Pressure, end.Pressure, amount),
            start.Tilt < 0 || end.Tilt < 0 ? -1 : Interp(start.Tilt, end.Tilt, amount),
            start.Orientation < 0 || end.Orientation < 0 ? -1 : InterpAngle(start.Orientation, end.Orientation, amount));

    public static float Distance(Vector2 start, Vector2 end) => (end - start).Length();

    public static float NearestPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
    {
        if (segmentStart == segmentEnd) return 0;

        Vector2 segmentVector = segmentEnd - segmentStart;
        Vector2 projectionVector = point - segmentStart;
        return Clamp01(Vector2.Dot(projectionVector, segmentVector) / Vector2.Dot(segmentVector, segmentVector));
    }

    public static Vector2? GetStrokeNormal(TipState tipState, TimeSpan prevTime)
    {
        const float cosineHalfDegree = 0.99996192f;
        static Vector2 Orthogonal(Vector2 v) => new(-v.Y, v.X);

        float velocityMagnitude = tipState.Velocity.Length();
        float accelerationMagnitude = tipState.Acceleration.Length();

        if (velocityMagnitude == 0 && accelerationMagnitude == 0) return null;
        if (velocityMagnitude == 0) return Orthogonal(tipState.Acceleration);
        if (accelerationMagnitude == 0) return Orthogonal(tipState.Velocity);

        if (MathF.Abs(Vector2.Dot(tipState.Velocity, tipState.Acceleration)) >
            cosineHalfDegree * velocityMagnitude * accelerationMagnitude)
            return Orthogonal(tipState.Velocity);

        TimeSpan deltaT = tipState.Time - prevTime;
        Vector2 strokeDir = Unit(tipState.Velocity) + Unit(tipState.Velocity + tipState.Acceleration * (float)deltaT.TotalSeconds);
        return Orthogonal(strokeDir);

        static Vector2 Unit(Vector2 v) => v / v.Length();
    }

    public static float? ProjectToSegmentAlongNormal(Vector2 segmentStart, Vector2 segmentEnd, Vector2 position, Vector2 strokeNormal)
    {
        static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

        Vector2 v = segmentEnd - segmentStart;
        float det = Cross(strokeNormal, v);
        if (det == 0) return null;

        Vector2 w = segmentStart - position;
        float param = Cross(w, strokeNormal) / det;
        if (param < 0 || param > 1) return null;
        return param;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 2 * Pi;
        while (angle > 2 * Pi) angle -= 2 * Pi;
        return angle;
    }
}
