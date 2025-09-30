using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

public class DeleteLayerCmd(IReadOnlyList<int> target) : CommandBase
{
    private readonly ImmutableArray<int> _targetPath = [..target];
    private Entity _targetE = Entity.Null;
    private readonly List<Node> _refNodes = [];

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_targetE);
    public override IEnumerable<GodotObject> UndoRefObjects => _refNodes;

    public override void Do()
    {
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        _targetE = tree.Root.RemoveDescendant(_targetPath);
        _targetE.Remove<ToSerializeTag>();
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_targetPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        var node = worldView.RemoveNodeAt(_targetPath);
        if(_refNodes.Count == 0) _refNodes.Add(node);
        
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var overlayNode = worldOverlay.RemoveNodeAt(_targetPath);
        if(_refNodes.Count == 1) _refNodes.Add(overlayNode);
    }

    public override void Undo()
    {
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var overlayNode = _refNodes[1];
        worldOverlay.InsertNodeAt(overlayNode, _targetPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        var node = _refNodes[0];
        worldView.InsertNodeAt(node, _targetPath);
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(_targetE, _targetPath);
        
        // Layer tree data
        _targetE.Add(new ToSerializeTag());
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.InsertDescendant(_targetPath, _targetE);
    }
}