using System.Runtime.Serialization;
using Frent;
using R3;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillLayerSetting
{
    [DataMember] public ObservableList<Entity> ReferenceLayerEs = [];
}