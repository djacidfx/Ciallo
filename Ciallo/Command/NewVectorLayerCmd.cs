using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class NewVectorLayerCmd : CommandBase
{
    private readonly List<int> _insertPath;

    public NewVectorLayerCmd(List<int> insertPath = null)
    {
        _insertPath = insertPath ?? [0];
    }

    public override void Do()
    {
        var tree = Document.Get<LayerTreeManager>();
        
        // Creation
        if (DoRefEntities.Count == 0)
        {
            var e = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"Layer {tree.Root.ChildCount+1}" },
            };
            e.Add(new VectorLayerSetting(), node, new ToSerializeTag());
            DoRefEntities.Add(e);
        }
        
        // Layer tree
        var layerE = DoRefEntities[0];
        tree.Root.InsertDescendant(_insertPath, layerE);
        
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(layerE, _insertPath);
    }

    public override void Undo()
    {
        // Layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_insertPath);
        
        // Layer Tree
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveDescendant(_insertPath);
    }
}