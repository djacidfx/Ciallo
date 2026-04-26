using System;
using System.Runtime.Serialization;
using Frent;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FilledPolygonSetting : IEquatable<FilledPolygonSetting>
{
    [DataMember] public ReactiveProperty<Entity> BrushE = new(default);

    public FilledPolygonSetting Clone()
    {
        return new FilledPolygonSetting
        {
            BrushE = { Value = BrushE.Value },
        };
    }

    public bool Equals(FilledPolygonSetting other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(BrushE.Value, other.BrushE.Value);
    }

    public override bool Equals(object obj) => obj is FilledPolygonSetting other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(BrushE.Value);
}