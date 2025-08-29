using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Core;

public partial class NewVectorLayerCmd(List<int> insertPath = null) : CommandBase
{
    private List<int> _insertPath = insertPath;
    private Entity _originalWorkingLayer = Entity.Null;

    public override void Do()
    {
        var tree = Document.Get<LayerTreeManager>();
        
        // Creation
        if (DestructionQueue.Count == 0)
        {
            var e = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"Layer {tree.Root.ChildCount+1}" },
            };
            e.Add(new VectorLayerSetting(), node, new ToSerializeTag());
            DestructionQueue.Add(e);
        }
        
        // Layer tree
        var layerE = DestructionQueue[0];
        _insertPath ??= [tree.Root.ChildCount];
        tree.Root.InsertDescendant(_insertPath, layerE);
        
        // Layer tree view
        var layerTreeControl = Document.Get<LayerTreeControl>();
        var layerControl = layerTreeControl.CreateLayerControl(layerE.Get<LayerTreeNode>());
        layerTreeControl.Insert(_insertPath, layerControl);
        
        // Selection
        var selection = Document.Get<SelectionManager>();
        _originalWorkingLayer = selection.WorkingLayer.Value;
        selection.WorkingLayer.Value = layerE;
    }

    public override void Undo()
    {
        // Layer Tree
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveDescendant(_insertPath);
        
        // Layer tree view
        var layerTreeControl = Document.Get<LayerTreeControl>();
        layerTreeControl.RemoveFree(_insertPath);
        
        // Selection
        var selection = Document.Get<SelectionManager>();
        selection.WorkingLayer.Value = _originalWorkingLayer;
    }
}