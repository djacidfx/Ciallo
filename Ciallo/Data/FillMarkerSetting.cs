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
    [DataMember(Order = 0)] public ReactiveProperty<Entity> MarkerBrushE = new(default);
    [DataMember(Order = 1)] public ReactiveProperty<Color?> MarkerOverrideColor = new(null);
    // Order = 2 for future FillMaterial entity
    [DataMember(Order = 3)] public ReactiveProperty<Color> FillColor = new(Colors.White);

    public FillMarkerSetting Clone()
    {
        return new FillMarkerSetting
        {
            MarkerBrushE = { Value = MarkerBrushE.Value },
            MarkerOverrideColor = { Value = MarkerOverrideColor.Value },
            FillColor = { Value = FillColor.Value },
        };
    }

    public bool Equals(FillMarkerSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(MarkerBrushE.Value, other.MarkerBrushE.Value)
               && Nullable.Equals(MarkerOverrideColor.Value, other.MarkerOverrideColor.Value)
               && FillColor.Value.Equals(other.FillColor.Value);
    }

    public override bool Equals(object? obj) => obj is FillMarkerSetting other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            MarkerBrushE.Value,
            MarkerOverrideColor.Value,
            FillColor.Value
        );
    }
}