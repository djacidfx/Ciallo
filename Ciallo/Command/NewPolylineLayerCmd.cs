using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

public class NewPolylineLayerCmd : CommandBase
{
    private Entity _layerE = Entity.Null;
    private readonly List<Node> _refObjects = [];

    public NewPolylineLayerCmd()
    {
        // Hierarchy not implemented, always add to root
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_layerE);
    public override IEnumerable<GodotObject> DoRefObjects => _refObjects;

    public override void Do()
    {
        InitEntity();

        // Data
        var tree = Document.Get<LayerTreeManager>();
        _layerE.Add(new ToSerializeTag());
        tree.Root.AddChild(_layerE);
        
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(_layerE);
        
        // View
        var worldView = Document.Get<WorldView>();
        if (_refObjects.Count == 0) _refObjects.Add(new PolylineLayerView());
        var layerView =  (PolylineLayerView)_refObjects[0];
        worldView.AddChild(layerView);
        _layerE.Add(layerView);
        
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        if(_refObjects.Count == 1) _refObjects.Add(new PolylineLayerOverlay());
        var layerOverlay = (PolylineLayerOverlay)_refObjects[1];
        worldOverlay.AddChild(layerOverlay);
        _layerE.Add(layerOverlay);
    }

    public override void Undo()
    {
        // Overlay
        var overlay = Document.Get<WorldOverlay>();
        _layerE.Remove<PolylineLayerOverlay>();
        overlay.RemoveChild(_refObjects[1]);
        
        // View
        _layerE.Remove<PolylineLayerView>();
        var worldView = Document.Get<WorldView>();
        worldView.RemoveChild(_refObjects[0]);
        
        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_layerE);
        
        // Data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveChild(^1);
        _layerE.Remove<ToSerializeTag>();
    }
    
    public Entity InitEntity()
    {
        var tree = Document.Get<LayerTreeManager>();
        
        if (_layerE == Entity.Null)
        {
            _layerE = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"{"Line layer".Tr()} {tree.Root.ChildCount+1}" },
            };
            _layerE.Add(new PolylineLayerSetting(), node);
        }

        return _layerE;
    }
}