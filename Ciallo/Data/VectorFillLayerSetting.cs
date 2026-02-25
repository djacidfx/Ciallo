using System.Collections.Immutable;
using System.Runtime.Serialization;
using Frent;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillLayerSetting
{
    [DataMember] public ReactiveProperty<ImmutableHashSet<Entity>> ReferenceLayers = new([]);

    public VectorFillLayerSetting Clone() => new()
    {
        ReferenceLayers = { Value = ReferenceLayers.Value },
    };
}