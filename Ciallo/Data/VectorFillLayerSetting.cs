using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillLayerSetting
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableHashSet<Entity> ReferenceLayers = [];
    public VectorFillLayerSetting Clone()
    {
        VectorFillLayerSetting newObj = new();
        newObj.ReferenceLayers.AddRange(ReferenceLayers);
        return newObj;
    }
}
