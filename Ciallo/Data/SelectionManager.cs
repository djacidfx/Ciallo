using System.Collections.Immutable;
using System.Runtime.Serialization;
using Arch.Core;
using Arch.Core.Extensions;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class SelectionManager
{
    [DataMember] public ObservableList<Entity> SelectedLayers = [];

    // Empty array represents no selection (or root node is selected).
    public ImmutableArray<int> WorkingLayerPath
    {
        get
        {
            if (WorkingLayer.Value == Entity.Null) return [];
            var world = AppWorldManager.GetWorldById(WorkingLayer.Value.WorldId);
            ImmutableArray<int> path = [..world.Document().Get<LayerTreeManager>().Root.SearchPathTo(WorkingLayer.Value)];
            return path;
        }
    }
    
    [DataMember] public ReactiveProperty<Entity> WorkingLayer = new(Entity.Null);
    
    [DataMember] public ReactiveProperty<Entity> WorkingBrush = new(Entity.Null);
}