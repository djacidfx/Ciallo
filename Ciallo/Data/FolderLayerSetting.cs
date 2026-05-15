using System.Runtime.Serialization;
using Frent;
using ObservableCollections;
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
    /// By design, cel folders cannot be nested each other, but can freely contain or be contained by regular folders
    /// which means at any path from root(document entity) to leaf, there must be at most one cel folder.
    ///
    /// Cel is pronounced in JP style "seru" (セ ル) or in its full name "celluloid"
    /// Don't pronounce it as "cell" since it is used to refer to grid cell positions on timeline tracks.
    /// </remarks>
    public bool IsCel
    {
        get => Exposures != null;
        set => Exposures = value ? (Exposures ?? []) : null;
    }

    /// <summary>
    /// Cel exposure table. 
    /// Keys represent the starting frame of a drawing; 
    /// Values represent the layer to be displayed until the next key is encountered.
    /// </summary>
    [DataMember] public ObservableSortedList<int, Entity> Exposures = null;

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            Exposures = Exposures is null ? null : [.. Exposures],
        };
}