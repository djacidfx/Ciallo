using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using Godot;

namespace Ciallo.Geometry;

[DataContract]
public readonly record struct BezierPoint(
    [property: DataMember(Order = 0)] Vector2 P,
    [property: DataMember(Order = 1)] Vector2 In,
    [property: DataMember(Order = 2)] Vector2 Out)
{
    [Pure] public BezierPoint WithIn(Vector2 newIn) => this with { In = newIn };
    [Pure] public BezierPoint WithOut(Vector2 newOut) => this with { Out = newOut };
    [Pure] public BezierPoint WithPoint(Vector2 newP) => this with { P = newP };

    // Absolute In
    public Vector2 PIn => P + In;
    // Absolute Out
    public Vector2 POut => P + Out;

    /// <remarks>
    /// Duck style design: As it is computed as a control mode, it's the control mode.
    /// </remarks>
    public HandleControlMode EstimatedHandleMode
    {
        get
        {
            bool isEqual = Mathf.IsEqualApprox(In.Length(), Out.Length());
            bool isLinear = Mathf.IsEqualApprox(MathF.Abs(In.Angle() - Out.Angle()), MathF.PI);
            if (isEqual && isLinear)
                return HandleControlMode.LinearEqual;
            if (isLinear)
                return HandleControlMode.Linear;
            return HandleControlMode.Free;
        }
    }

    [Pure] public BezierPoint WithHandleMode(HandleControlMode mode) =>
        mode switch
        {
            HandleControlMode.LinearEqual => this with { In = -Out },
            HandleControlMode.Linear => this with { In = -Out.Normalized() * In.Length() },
            HandleControlMode.Free => this,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}

/// <summary>
/// Shen: Godot's Curve2D's tangent mode is very, very unintuitive. I guess who programed it have never used Adobe Illustrator or Inkscape.
/// Sep 17, 2025. Shen: Seem fixed in 4.5? No, only available in animation editor, not for runtime.
/// </summary>
public enum HandleControlMode
{
    LinearEqual, // Two handles are equal in length and tangent (opposite direction)
    Linear, // Equal in tangent only
    Free
}