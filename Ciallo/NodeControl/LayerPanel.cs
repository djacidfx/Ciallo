using Godot;
using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using R3;

public partial class LayerPanel : VBoxContainer
{
    public static readonly PackedScene LayerTreeScene = GD.Load<PackedScene>("res://NodeControl/LayerTree.tscn");

    private Control _visibleLayerTreeControl;
    
    public override void _Ready()
    {
        GetNode<Node>("%LayerTreePreview").QueueFree();
        WorldManager.WorkingWorld.Skip(1).Subscribe(w =>
        {
            if(w == null && _visibleLayerTreeControl != null)
            {
                _visibleLayerTreeControl.Visible = false;
                _visibleLayerTreeControl = null;
                return;
            }
            if (_visibleLayerTreeControl != null) _visibleLayerTreeControl.Visible = false;
            var doc = w.Document();
            _visibleLayerTreeControl = doc.Get<LayerTreeContainer>();
            _visibleLayerTreeControl.Visible = true;
        }).AddTo(this);
    }

    public void CreateAddLayerTreeControl(Entity document)
    {
        var layerTreeControl = LayerTreeScene.Instantiate<LayerTreeContainer>();
        layerTreeControl.Visible = false;
        AddChild(layerTreeControl);
        document.Add(layerTreeControl);
    }

    public void RemoveFreeLayerTreeControl(Entity document)
    {
        var layerTreeControl = document.Get<LayerTreeContainer>();
        if(_visibleLayerTreeControl == layerTreeControl)
            _visibleLayerTreeControl = null;
        document.Remove<LayerTreeContainer>();
        layerTreeControl.QueueFree();
    }
}
