using System.Runtime.Serialization;
using Frent;
using ObservableCollections;
using R3;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class SelectionManager
{
    /// <summary>Current playhead position.</summary>
    [DataMember] public ReactiveProperty<int> CurrentFrame = new(1);

    [DataMember] public ObservableList<Entity> SelectedLayers = [];

    [DataMember] public ReactiveProperty<Entity> WorkingLayer = new(Entity.Null);
    public ReadOnlyReactiveProperty<Entity> WorkingCelFolder;

    [DataMember] public ReactiveProperty<Entity> WorkingStrokeBrush = new(Entity.Null);

    [DataMember] public ReactiveProperty<Entity> WorkingVectorFillBrush = new(Entity.Null);

    public ObservableList<Entity> SelectedShapes = [];

    public SelectionManager()
    {
        WorkingCelFolder = WorkingLayer.Select(layerE =>
        {
            if (layerE.IsNull || layerE.IsDocument)
                return Entity.Null;
            if (layerE.TryGet<FolderLayerSetting>()?.IsCel == true)
            {
                return layerE;
            }

            var ancestors = layerE.Get<LayerTreeNode>().EnumerateAncestors();
            foreach (Entity e in ancestors)
            {
                // Layer's parent must have FolderLayerSetting component, but it may not be a cel folder.
                if (e.Get<FolderLayerSetting>().IsCel) return e;
            }

            return Entity.Null;
        }).ToReadOnlyReactiveProperty();
    }
}