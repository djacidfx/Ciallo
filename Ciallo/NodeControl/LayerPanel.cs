using Godot;
using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using R3;

/// <summary>
/// Show layers, toggle LayerTree scenes' visibility according to current working document
/// </summary>
public partial class LayerPanel : VBoxContainer
{
    private Control _visibleLayerTree;
    
    public override void _Ready()
    {
        GetNode<Node>("%LayerContainerPreview").QueueFree();
        WorldManager.WorkingWorld.Skip(1).Subscribe(w =>
        {
            if(w == null && _visibleLayerTree != null)
            {
                _visibleLayerTree.Visible = false;
                _visibleLayerTree = null;
                return;
            }
            if (_visibleLayerTree != null) _visibleLayerTree.Visible = false;
            var doc = w.Document();
            _visibleLayerTree = doc.Get<LayerContainer>();
            _visibleLayerTree.Visible = true;
        }).AddTo(this);
    }

    public void CreateAddLayerContainer(Entity document)
    {
        var layerContainer = LayerContainer.Instantiate();
        layerContainer.Visible = false;
        AddChild(layerContainer);
        document.Add(layerContainer);
    }

    public void RemoveFreeLayerContainer(Entity document)
    {
        var layerTreeControl = document.Get<LayerContainer>();
        if(_visibleLayerTree == layerTreeControl)
            _visibleLayerTree = null;
        document.Remove<LayerContainer>();
        layerTreeControl.QueueFree();
    }
}
