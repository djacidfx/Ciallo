using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FolderLayerSetting
{
    [DataMember] public ReactiveProperty<bool> IsFolded = new(false);

    public FolderLayerSetting Clone() => new()
    {
        IsFolded = { Value = IsFolded.Value }
    };
}