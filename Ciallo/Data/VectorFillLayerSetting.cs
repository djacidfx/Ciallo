using System.Runtime.Serialization;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillLayerSetting
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableHashSet<Entity> ReferenceLayers = [];
    [DataMember, ProjectField(StorageKind.Blob)]
    public ReactiveProperty<Color?> BoundedColor = new();

    public VectorFillLayerSetting Clone()
    {
        VectorFillLayerSetting newObj = new();
        newObj.ReferenceLayers.AddRange(ReferenceLayers);
        newObj.BoundedColor.Value = BoundedColor.Value;
        return newObj;
    }
}
