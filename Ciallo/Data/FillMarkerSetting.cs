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
    [DataMember] public ReactiveProperty<Entity> MarkerBrushE = new(default);
    [DataMember] public ReactiveProperty<Color?> MarkerOverridingColor = new(null);

    public FillMarkerSetting Clone()
    {
        return new FillMarkerSetting
        {
            MarkerBrushE = { Value = MarkerBrushE.Value },
            MarkerOverridingColor = { Value = MarkerOverridingColor.Value },
        };
    }

    public bool Equals(FillMarkerSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(MarkerBrushE.Value, other.MarkerBrushE.Value)
               && Nullable.Equals(MarkerOverridingColor.Value, other.MarkerOverridingColor.Value);
    }

    public override bool Equals(object obj) => obj is FillMarkerSetting other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            MarkerBrushE.Value,
            MarkerOverridingColor.Value
        );
    }
}