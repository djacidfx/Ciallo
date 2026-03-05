using System;
using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FillMarkerSetting : IEquatable<FillMarkerSetting>
{
    // Marker use stroke/polyline has single point
    [DataMember(Order = 0)] public ReactiveProperty<Entity> StrokeBrushE = new(default);
    [DataMember(Order = 1)] public ReactiveProperty<Color?> StrokeOverrideColor = new(null);
    // Order = 2 for future FillMaterial entity
    [DataMember(Order = 3)] public ReactiveProperty<Color> FillColor = new(Colors.White);

    public FillMarkerSetting Clone()
    {
        return new FillMarkerSetting
        {
            StrokeBrushE = { Value = StrokeBrushE.Value },
            StrokeOverrideColor = { Value = StrokeOverrideColor.Value },
            FillColor = { Value = FillColor.Value },
        };
    }

    public bool Equals(FillMarkerSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(StrokeBrushE.Value, other.StrokeBrushE.Value)
               && Nullable.Equals(StrokeOverrideColor.Value, other.StrokeOverrideColor.Value)
               && FillColor.Value.Equals(other.FillColor.Value);
    }

    public override bool Equals(object? obj) => obj is FillMarkerSetting other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StrokeBrushE.Value,
            StrokeOverrideColor.Value,
            FillColor.Value
        );
    }
}