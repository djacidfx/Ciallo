using System;
using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillMarkerSetting : IEquatable<VectorFillMarkerSetting>
{
    // Marker use stroke/polyline has single point
    [DataMember] public ReactiveProperty<Entity> BrushE = new(default);
    [DataMember] public ReactiveProperty<Color?> MarkerOverridingColor = new(null);

    public VectorFillMarkerSetting Clone()
    {
        return new VectorFillMarkerSetting
        {
            BrushE = { Value = BrushE.Value },
            MarkerOverridingColor = { Value = MarkerOverridingColor.Value },
        };
    }

    public bool Equals(VectorFillMarkerSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(BrushE.Value, other.BrushE.Value)
               && Nullable.Equals(MarkerOverridingColor.Value, other.MarkerOverridingColor.Value);
    }

    public override bool Equals(object obj) => obj is VectorFillMarkerSetting other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            BrushE.Value,
            MarkerOverridingColor.Value
        );
    }
}