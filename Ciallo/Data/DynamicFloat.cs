using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Ciallo.Geometry;
using Godot;
using R3;

namespace Ciallo.Data;

/// <summary>
/// A field in brush setting allow varying over inputs like pen pressure, tilt...
/// </summary>
[DataContract]
public class DynamicFloat
{
    [DataMember] public ReactiveProperty<float> Value = new();

    [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> Pressure = new();

    [DataMember] public ReactiveProperty<Vector2?> RandomMinMax = new();

    // Material part is not implemented yet.
    // [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> TiltRadius = new(); // Polar coordinate
    // [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> TiltAngle = new();


    public DynamicFloat(float initialValue = 0.0f) => Value.Value = initialValue;
}

// Tell shader which input is used.
// The sensor input type will be less than 10-20 so store their types into bitflags.
[Flags]
public enum DynamicFloatFlags
{
    None = 0,
    Pressure = 1 << 0,
    Random = 1 << 1,
}