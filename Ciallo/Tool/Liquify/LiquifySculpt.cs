using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Tool;

public static class LiquifySculpt
{
    public static Vector2 Apply(
        LiquifyMode mode,
        Vector2 position,
        int pointIndex,
        Vector2 brushCenter,
        Vector2 brushDelta,
        float radius,
        float strength,
        float pressure,
        IReadOnlyList<Vector2> strokePositions)
    {
        float influence = Influence(position, brushCenter, radius, strength, NormalizePressure(pressure));
        if (influence <= 0f)
            return position;

        return mode switch
        {
            LiquifyMode.Push => Push(position, brushDelta, influence),
            LiquifyMode.Expand => Expand(position, brushCenter, influence),
            LiquifyMode.Pinch => Pinch(position, brushCenter, influence),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public static float Influence(Vector2 position, Vector2 brushCenter, float radius, float strength, float pressure)
    {
        float distance = position.DistanceTo(brushCenter);
        if (distance > radius)
            return 0f;

        float normalizedDistance = distance / radius;
        float falloff = SmoothFalloff(normalizedDistance);
        return strength * pressure * falloff;
    }

    private static Vector2 Push(Vector2 position, Vector2 brushDelta, float influence)
    {
        return position + brushDelta * influence;
    }

    private static Vector2 Expand(Vector2 position, Vector2 brushCenter, float influence)
    {
        float inf = influence / 5f;
        float factor = 1f + inf * inf;
        return brushCenter + (position - brushCenter) * factor;
    }

    private static Vector2 Pinch(Vector2 position, Vector2 brushCenter, float influence)
    {
        float inf = influence / 5f;
        float factor = 1f - inf * inf;
        return brushCenter + (position - brushCenter) * factor;
    }

    private static float SmoothFalloff(float normalizedDistance)
    {
        float t = Mathf.Clamp(normalizedDistance, 0f, 1f);
        return 1f - t * t * (3f - 2f * t);
    }

    private static float NormalizePressure(float pressure) => pressure <= 0f ? 1f : pressure;
}
