using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FolderLayerSetting
{
    [DataMember] public ReactiveProperty<bool> IsExpanded = new(true);
    /// <summary>
    /// When true this folder acts as a frame-by-frame (celluloid) animation track.
    /// Its children are treated as cels.
    /// </summary>
    [DataMember] public ReactiveProperty<bool> IsCelFolder = new(false);

    public bool IsCel => IsCelFolder.Value;

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            IsCelFolder = { Value = IsCelFolder.Value }
        };
}