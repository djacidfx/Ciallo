using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableList<Entity> StrokeBrushEs = [];
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableList<Entity> VectorFillBrushEs = [];
}
