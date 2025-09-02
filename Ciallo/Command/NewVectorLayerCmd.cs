using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

public partial class NewVectorLayerCmd : CommandBase
{
    private List<int> _insertPath;
    private Entity _originalWorkingLayer = Entity.Null;

    public NewVectorLayerCmd()
    {
    }

    public NewVectorLayerCmd(List<int> insertPath = null)
    {
        _insertPath = insertPath;
    }

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

        _insertPath ??= [0];
        // Layer tree
        var layerE = DestructionQueue[0];
        tree.Root.InsertDescendant(_insertPath, layerE);
        
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(layerE, _insertPath);
        
        // Selection
        var selection = Document.Get<SelectionManager>();
        _originalWorkingLayer = selection.WorkingLayer.Value;
        selection.WorkingLayer.Value = layerE;
    }

    public override void Undo()
    {
        // Selection
        var selection = Document.Get<SelectionManager>();
        selection.WorkingLayer.Value = _originalWorkingLayer;
        
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_insertPath);
        
        // Layer Tree
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveDescendant(_insertPath);
    }
}