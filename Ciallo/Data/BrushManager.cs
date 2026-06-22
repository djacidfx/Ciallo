using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableList<Entity> StrokeBrushes = [];
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableList<Entity> VectorFillBrushes = [];
}
