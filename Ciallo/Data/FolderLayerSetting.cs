using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FolderLayerSetting
{
    [DataMember] public ReactiveProperty<bool> IsExpanded = new(true);
    /// <summary>
    /// When true this folder acts as an animation track.
    /// Its direct children that carry <see cref="CelFrameSetting"/> are treated as cels.
    /// </summary>
    [DataMember] public ReactiveProperty<bool> IsAnimationFolder = new(false);

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            IsAnimationFolder = { Value = IsAnimationFolder.Value }
        };
}