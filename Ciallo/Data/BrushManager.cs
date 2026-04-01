using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    [DataMember] public ObservableList<Entity> StrokeBrushEs = [];
    [DataMember] public ObservableList<Entity> VectorFillBrushEs = [];
}