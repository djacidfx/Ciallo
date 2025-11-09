using Godot;

namespace Ciallo.Misc;

public static class FloatRemap
{
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
    }

    public static float SmoothRemap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = Mathf.InverseLerp(fromMin, fromMax, value);
        t = t * t * (3f - 2f * t); // Smoothstep
        return Mathf.Lerp(toMin, toMax, t);
    }

    // Map input to [toMin, toMax], the sensitive range has wider change rate.
    // Different from SmoothRemap, this can map toMin/toMax even if value is out of sensitive range.
    // Take 95% value domine [-3.6635, 3.6635] as sensitive value range.
    public static float SigmoidRemap(this float value, float sensitiveMin, float sensitiveMax, float toMin, float toMax)
    {
        float t = Mathf.InverseLerp(sensitiveMin, sensitiveMax, value);
        t = t * 2 - 1; // Map to [-1, 1]
        t *= 3.6635f;
        t = 1f / (1f + Mathf.Exp(-t)); // Sigmoid
        return Mathf.Lerp(toMin, toMax, t);
    }
}