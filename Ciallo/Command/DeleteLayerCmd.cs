using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

public partial class DeleteLayerCmd(ImmutableArray<int> target) : CommandBase
{
    public override void Do()
    {
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        var e = tree.Root.RemoveDescendant(target);
        if(UndoRefEntities.Count == 0) UndoRefEntities.Add(e);
        
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(target);
    }

    public override void Undo()
    {
        var e = UndoRefEntities.First();
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(e, target);
        
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.InsertDescendant(target, e);
    }
}