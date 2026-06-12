using System.Runtime.Serialization;
using System.Collections.Immutable;
using Frent;
using ObservableCollections;
using R3;
using System.Collections.Generic;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FolderLayerSetting
{
    [DataMember, ProjectField] public ReactiveProperty<bool> IsExpanded = new(true);
    /// <summary>
    /// When true this folder acts as a frame-by-frame (celluloid) animation track.
    /// Its children are treated as cels going to be placed on trace.
    /// </summary>
    /// <remarks>
    /// By design, cel folders cannot be nested each other, but can freely contain or be contained by regular folders
    /// which means at any path from root(document entity) to leaf, there must be at most one cel folder.
    ///
    /// Cel is pronounced in JP style "seru" (セ ル) or in its full name "celluloid".
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
    [DataMember, ProjectField(StorageKind.Entity, EntityNullability.Required)]
    public ObservableSortedList<int, Entity> Exposures = null;
    public ReadOnlyReactiveProperty<Entity> CurrentExposedCel { get; private set; }
    /// <summary>
    /// Onion skin cels keyed by exposure-index offset.
    /// </summary>
    public ReadOnlyReactiveProperty<SortedList<int, Entity>> CurrentOnionSkinCels { get; private set; }
    public void InitCurrent(ReactiveProperty<int> currentFrame, Observable<ImmutableArray<int>> onionSkinOffsets)
    {
        CurrentExposedCel = Exposures.ObserveChanged().PrependDefault()
            .CombineLatest(currentFrame, (_, currentFrame) => Exposures.FloorIndex(currentFrame))
            .Select(idx => idx >= 0 ? Exposures.GetValueAtIndex(idx) : Entity.Null)
            .ToReadOnlyReactiveProperty();

        CurrentOnionSkinCels = Exposures.ObserveChanged().PrependDefault()
            .CombineLatest(onionSkinOffsets, currentFrame,
                (_, offsets, frame) => BuildCurrentOnionSkinCels(frame, offsets))
            .ToReadOnlyReactiveProperty();
    }

    private SortedList<int, Entity> BuildCurrentOnionSkinCels(int currentFrame, ImmutableArray<int> offsets)
    {
        var cels = new SortedList<int, Entity>();
        int currentExposureIndex = Exposures.FloorIndex(currentFrame);
        if (currentExposureIndex < 0)
            return cels;

        foreach (var offset in offsets)
        {
            int targetExposureIndex = currentExposureIndex + offset;
            if (targetExposureIndex < 0 || targetExposureIndex >= Exposures.Count)
                continue;

            cels[offset] = Exposures.GetValueAtIndex(targetExposureIndex);
        }

        return cels;
    }

    /// <summary>
    /// The working layer under a selected cel is determined by this path.
    /// This is runtime preference state and is not serialized.
    /// 
    /// The path is relative to the selected cel root; an empty path means the selected cel itself.
    /// Invalid path indexes are resolved to the nearest preorder node without mutating this preference.
    /// The default [-1, -1] prefers the last child of the last child/folder under the selected cel.
    /// </summary>
    public ReactiveProperty<ImmutableArray<int>> PreferredWorkingLayerPathForCelSelection = new([-1, -1]);

    public FolderLayerSetting Clone() =>
        new()
        {
            IsExpanded = { Value = IsExpanded.Value },
            Exposures = Exposures is null ? null : [.. Exposures],
            PreferredWorkingLayerPathForCelSelection = { Value = PreferredWorkingLayerPathForCelSelection.Value },
        };
}
