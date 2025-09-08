using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Rendering;

namespace Ciallo.Command;

public class NewStrokeCmd(IReadOnlyList<int> insertPath) : CommandBase
{
    private readonly ImmutableArray<int> _insertPath = [..insertPath];
    
    public override void Do()
    {
        var tree = Document.Get<LayerTreeManager>();
        // Creation
        if (DoRefEntities.Count == 0)
        {
            var e = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
            };
            e.Add(new StrokeGeometry(), node, new ToSerializeTag());
            DoRefEntities.Add(e);
        }
        
        // Layer tree data
        var strokeE = DoRefEntities[0];
        tree.Root.InsertDescendant(_insertPath, strokeE);
        
        // View
        var worldView = Document.Get<WorldView>();
        if (DoRefObjects.Count == 0) DoRefObjects.Add(new StrokeView());
        var strokeView =  (StrokeView)DoRefObjects[0];
        worldView.InsertNodeAt(strokeView, _insertPath);
        strokeE.Add(strokeView);
    }

    public override void Undo()
    {
        var strokeE = DoRefEntities[0];
        var tree = Document.Get<LayerTreeManager>();
        // View
        var worldView = Document.Get<WorldView>();
        strokeE.Remove<StrokeView>();
        worldView.RemoveNodeAt(_insertPath);
        
        // Layer tree data
        tree.Root.RemoveDescendant(_insertPath);
    }
}