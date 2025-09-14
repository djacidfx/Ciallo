using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public class SelectionManager
{
    public readonly ObservableList<Entity> SelectedLayers = [];

    // Empty array represents no selection (or root node is selected).
    public ImmutableArray<int> WorkingLayerPath
    {
        get
        {
            if (WorkingLayer == Entity.Null) return [];
            var world = AppWorldManager.GetWorldById(_workingLayer.WorldId);
            ImmutableArray<int> path = [..world.Document().Get<LayerTreeManager>().Root.SearchPathTo(WorkingLayer)];
            return path;
        }
    }

    private Entity _workingLayer = Entity.Null;
    public Entity WorkingLayer
    {
        get => _workingLayer;
        set
        {
            if (_workingLayer == value) return;
            _workingLayer = value;
            WorkingLayerChanged.OnNext(_workingLayer);
        }
    }

    public readonly Subject<Entity> WorkingLayerChanged = new();
}