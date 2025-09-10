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
        
        // Data
        var strokeE = DoRefEntities[0];
        tree.Root.InsertDescendant(_insertPath, strokeE);
        
        // View
        var view = Document.Get<WorldView>();
        if (DoRefObjects.Count == 0) DoRefObjects.Add(new StrokeView());
        var strokeView =  (StrokeView)DoRefObjects[0];
        view.InsertNodeAt(strokeView, _insertPath);
        strokeE.Add(strokeView);
        
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        if(DoRefObjects.Count == 1) DoRefObjects.Add(new StrokeOverlay());
        var strokeOverlay = (StrokeOverlay)DoRefObjects[1];
        overlay.InsertNodeAt(strokeOverlay, _insertPath);
        strokeE.Add(strokeOverlay);
    }

    public override void Undo()
    {
        var strokeE = DoRefEntities[0];
        var tree = Document.Get<LayerTreeManager>();
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        strokeE.Remove<StrokeOverlay>();
        overlay.RemoveNodeAt(_insertPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        strokeE.Remove<StrokeView>();
        worldView.RemoveNodeAt(_insertPath);
        
        // Data
        tree.Root.RemoveDescendant(_insertPath);
    }
}