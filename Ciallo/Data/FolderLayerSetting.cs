using System.Runtime.Serialization;
using System.Collections.Immutable;
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
    public ReadOnlyReactiveProperty<Entity> CurrentExposedCel { get; private set; }
    public void InitCurrentExposedCel(ReactiveProperty<int> currentFrame)
    {
        CurrentExposedCel = Exposures.ObserveChanged().PrependDefault()
            .CombineLatest(currentFrame, (_, currentFrame) => Exposures.FloorIndex(currentFrame))
            .Select(idx => idx >= 0 ? Exposures.GetValueAtIndex(idx) : Entity.Null)
            .ToReadOnlyReactiveProperty();
    }

    /// <summary>
    /// After rolling the timeline, the working layer under the exposed cel is determined by this path.
    /// This is runtime preference state and is not serialized.
    /// 
    /// The path is relative to the exposed cel root; an empty path means the exposed cel itself.
    /// Invalid path indexes are resolved to the nearest preorder node without mutating this preference.
    /// The default [-1, -1] prefers the last child of the last child/folder under the exposed cel.
    /// </summary>
    public ReactiveProperty<ImmutableArray<int>> PreferredWorkingLayerPathOnRollingFrame = new([-1, -1]);

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            Exposures = Exposures is null ? null : [.. Exposures],
            PreferredWorkingLayerPathOnRollingFrame = { Value = PreferredWorkingLayerPathOnRollingFrame.Value },
        };
}
