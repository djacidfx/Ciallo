using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeSetting
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Nullable)]
    public ReactiveProperty<Entity> Brush = new(default);
    [DataMember, ProjectField] public ReactiveProperty<Color?> OverrideColor = new();

    public StrokeSetting Clone()
    {
        return new StrokeSetting
        {
            Brush = { Value = Brush.Value },
            OverrideColor = { Value = OverrideColor.Value },
        };
    }
}
