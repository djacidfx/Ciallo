using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeSetting
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Nullable)]
    public ReactiveProperty<Entity> BrushE = new(default);
    [DataMember, ProjectField(StorageKind.Blob)] public ReactiveProperty<Color?> OverrideColor = new();

    public StrokeSetting Clone()
    {
        return new StrokeSetting
        {
            BrushE = { Value = BrushE.Value },
            OverrideColor = { Value = OverrideColor.Value },
        };
    }
}
