using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FolderLayerSetting
{
    [DataMember] public ReactiveProperty<bool> IsExpanded = new(true);
    /// <summary>
    /// When true this folder acts as a frame-by-frame (celluloid) animation track.
    /// Its children are treated as cels going to be placed on trace.
    /// </summary>
    /// <remarks>
    /// By design, cel folder cannot be nested in any hierarchy, but can freely contain or be contained by regular folders
    /// which means at any path from root(document enitity) to leaf, there must be at most one cel folder.
    /// </remarks>
    [DataMember] public ReactiveProperty<bool> IsCelFolder = new(false);
    public bool IsCel => IsCelFolder.Value;

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            IsCelFolder = { Value = IsCelFolder.Value }
        };
}