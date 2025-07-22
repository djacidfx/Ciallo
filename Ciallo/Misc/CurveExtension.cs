/* This file is originally copied from Godot 4.4 curve.cpp, translated to C# with AI tool.*/

using Godot;

namespace Ciallo.Misc;

public static class CurveExtension
{
    public static float SampleLocalNoCheck(this Curve curve, int index, float localOffset)
    {
        if (curve == null || index < 0 || index >= curve.GetPointCount() - 1)
        {
            GD.PushError("Invalid index or null curve in SampleLocalNoCheck.");
            return 0f;
        }

        Vector2 a = curve.GetPointPosition(index);
        Vector2 b = curve.GetPointPosition(index + 1);

        float d = b.X - a.X;
        if (Mathf.IsZeroApprox(d))
        {
            return b.Y;
        }

        localOffset /= d;
        d /= 3.0f;
        float yac = a.Y + d * curve.GetPointRightTangent(index);
        float ybc = b.Y - d * curve.GetPointLeftTangent(index + 1);

        float y = Mathf.BezierInterpolate(a.Y, yac, ybc, b.Y, localOffset);

        return y;
    }
}