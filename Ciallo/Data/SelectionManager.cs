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

    // Null is allowed to represent no selection. Empty array represent root node and should not be the working layer. 
    private int[] _workingLayerPath;
    public IReadOnlyList<int> WorkingLayerPath
    {
        get => _workingLayerPath;
        set
        {
            if(value != null && value.Count == 0)
                throw new System.ArgumentException("Working layer cannot be root node.");
            if (ReferenceEquals(_workingLayerPath, value)) return;
            if (value != null && _workingLayerPath?.SequenceEqual(value) == true) return;
            _workingLayerPath = value?.ToArray();
            WorkingLayerChanged.OnNext(WorkingLayer);
        }
    }

    public Entity WorkingLayer => _workingLayerPath == null ? Entity.Null : 
        WorldManager.WorkingDocument.Get<LayerTreeManager>().Root.GetDescendantEntity(WorkingLayerPath);

    public readonly Subject<Entity> WorkingLayerChanged = new();
}