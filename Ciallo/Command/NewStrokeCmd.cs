using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

public class NewStrokeCmd(IReadOnlyList<int> insertPath, Entity brushE) : CommandBase
{
    private readonly ImmutableArray<int> _insertPath = [..insertPath];
    private Entity _strokeE = Entity.Null;
    private readonly List<Node> _refNodes = [];
    
    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_strokeE);
    public override IEnumerable<GodotObject> DoRefObjects => _refNodes;

    public override void Do()
    {
        // Creation
        if (_strokeE == Entity.Null)
        {
            _strokeE = WorkingWorld.Create();
            var node = new LayerTreeNode();
            _strokeE.Add(new StrokeGeometry(), node);
            _strokeE.Add<BrushEntity>(brushE);
        }

        // Data
        var tree = Document.Get<LayerTreeManager>();
        _strokeE.Add(new ToSerializeTag());
        tree.Root.InsertDescendant(_insertPath, _strokeE);
        
        // View
        var view = Document.Get<WorldView>();
        if (_refNodes.Count == 0) _refNodes.Add(new StrokeView());
        var strokeView =  (StrokeView)_refNodes[0];
        view.InsertNodeAt(strokeView, _insertPath);
        _strokeE.Add(strokeView);
        strokeView.Material = brushE.Get<BrushMaterial>();
        
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        if(_refNodes.Count == 1) _refNodes.Add(new StrokeOverlay());
        var strokeOverlay = (StrokeOverlay)_refNodes[1];
        overlay.InsertNodeAt(strokeOverlay, _insertPath);
        _strokeE.Add(strokeOverlay);
    }

    public override void Undo()
    {
        var tree = Document.Get<LayerTreeManager>();
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        _strokeE.Remove<StrokeOverlay>();
        overlay.RemoveNodeAt(_insertPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        var strokeView = _strokeE.Get<StrokeView>();
        strokeView.Material = null;
        _strokeE.Remove<StrokeView>();
        worldView.RemoveNodeAt(_insertPath);
        
        // Data
        tree.Root.RemoveDescendant(_insertPath);
        _strokeE.Remove<ToSerializeTag>();
    }
}