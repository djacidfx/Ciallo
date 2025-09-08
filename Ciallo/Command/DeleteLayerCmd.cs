using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

public partial class DeleteLayerCmd(IReadOnlyList<int> target) : CommandBase
{
    private readonly ImmutableArray<int> _target = [..target];
    
    public override void Do()
    {
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        var e = tree.Root.RemoveDescendant(_target);
        if(UndoRefEntities.Count == 0) UndoRefEntities.Add(e);
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_target);
        
        // View
        var worldView = Document.Get<WorldView>();
        var node = worldView.RemoveNodeAt(_target);
        if(UndoRefObjects.Count == 0) UndoRefObjects.Add(node);
    }

    public override void Undo()
    {
        // View
        var worldView = Document.Get<WorldView>();
        var node = (Node)UndoRefObjects.First();
        worldView.InsertNodeAt(node, _target);
        
        var e = UndoRefEntities.First();
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(e, _target);
        
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.InsertDescendant(_target, e);
    }
}