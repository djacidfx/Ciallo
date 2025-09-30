using System.Collections.Generic;
using System.Collections.Immutable;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

public class NewPolylineLayerCmd(IReadOnlyList<int> insertPath) : CommandBase
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
                Name = { Value = $"{TranslationServer.Translate("Line layer")} {tree.Root.ChildCount+1}" },
            };
            _layerE.Add(new PolylineLayerSetting(), node);
        }
        
        // Layer tree data
        _layerE.Add(new ToSerializeTag());
        tree.Root.InsertDescendant(_insertPath, _layerE);
        
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(_layerE, _insertPath);
        
        // View
        var worldView = Document.Get<WorldView>();
        if (_refObjects.Count == 0) _refObjects.Add(new PolylineLayerView());
        var layerView =  (PolylineLayerView)_refObjects[0];
        worldView.InsertNodeAt(layerView, _insertPath);
        _layerE.Add(layerView);
        
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        if(_refObjects.Count == 1) _refObjects.Add(new PolylineLayerOverlay());
        var layerOverlay = (PolylineLayerOverlay)_refObjects[1];
        worldOverlay.InsertNodeAt(layerOverlay, _insertPath);
        _layerE.Add(layerOverlay);
    }

    public override void Undo()
    {
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        _layerE.Remove<PolylineLayerOverlay>();
        overlay.RemoveNodeAt(_insertPath);
        
        // View
        _layerE.Remove<PolylineLayerView>();
        var worldView = Document.Get<WorldView>();
        worldView.RemoveNodeAt(_insertPath);
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_insertPath);
        
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveDescendant(_insertPath);
        _layerE.Remove<ToSerializeTag>();
    }
}