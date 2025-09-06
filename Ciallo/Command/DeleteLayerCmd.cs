using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

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
        
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_target);
    }

    public override void Undo()
    {
        var e = UndoRefEntities.First();
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(e, _target);
        
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.InsertDescendant(_target, e);
    }
}