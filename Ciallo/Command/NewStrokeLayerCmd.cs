using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public class NewStrokeLayerCmd(IReadOnlyList<int> insertPath) : CommandBase
{
    private readonly ImmutableArray<int> _insertPath = [..insertPath];
    private Entity _layerE = Entity.Null;
    private readonly List<Node> _refObjects = [];

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_layerE);
    public override IEnumerable<GodotObject> DoRefObjects => _refObjects;

    public override void Do()
    {
        var tree = Document.Get<LayerTreeManager>();
        
        // Creation
        if (_layerE == Entity.Null)
        {
            _layerE = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"Stroke Layer {tree.Root.ChildCount+1}" },
            };
            _layerE.Add(new StrokeLayerSetting(), node, new ToSerializeTag());
        }
        
        // Layer tree data
        tree.Root.InsertDescendant(_insertPath, _layerE);
        
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(_layerE, _insertPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        if (_refObjects.Count == 0) _refObjects.Add(new StrokeLayerView());
        var layerView =  (StrokeLayerView)_refObjects[0];
        worldView.InsertNodeAt(layerView, _insertPath);
        _layerE.Add(layerView);
        
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        if(_refObjects.Count == 1) _refObjects.Add(new StrokeLayerOverlay());
        var layerOverlay = (StrokeLayerOverlay)_refObjects[1];
        worldOverlay.InsertNodeAt(layerOverlay, _insertPath);
        _layerE.Add(layerOverlay);
    }

    public override void Undo()
    {
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        _layerE.Remove<StrokeLayerOverlay>();
        overlay.RemoveNodeAt(_insertPath);
        
        // View
        _layerE.Remove<StrokeLayerView>();
        var worldView = Document.Get<WorldView>();
        worldView.RemoveNodeAt(_insertPath);
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_insertPath);
        
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveDescendant(_insertPath);
    }
}