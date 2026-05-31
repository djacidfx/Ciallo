using System;
using System.Runtime.Serialization;
using Frent;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillMarkerSetting : IEquatable<VectorFillMarkerSetting>
{
    // Marker use stroke/polyline has single point
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Nullable)]
    public ReactiveProperty<Entity> BrushE = new(default);

    public VectorFillMarkerSetting Clone()
    {
        return new VectorFillMarkerSetting
        {
            BrushE = { Value = BrushE.Value },
        };
    }

    public bool Equals(VectorFillMarkerSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(BrushE.Value, other.BrushE.Value);
    }

    public override bool Equals(object obj) => obj is VectorFillMarkerSetting other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            BrushE.Value
        );
    }
}
