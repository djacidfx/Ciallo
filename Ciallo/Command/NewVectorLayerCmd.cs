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
public class NewVectorLayerCmd : CommandBase
{
    private readonly ImmutableArray<int> _insertPath;

    public NewVectorLayerCmd(IReadOnlyList<int> insertPath)
    {
        _insertPath = [..insertPath];
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
        
        // Layer tree data
        var layerE = DoRefEntities[0];
        tree.Root.InsertDescendant(_insertPath, layerE);
        
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(layerE, _insertPath);
        
        // World view
        var worldView = Document.Get<WorldView>();
        if (DoRefObjects.Count == 0) DoRefObjects.Add(new VectorLayerView());
        var layerView =  (VectorLayerView)DoRefObjects[0];
        worldView.InsertNodeAt(layerView, _insertPath);
        layerE.Add(layerView);
    }

    public override void Undo()
    {
        var layerE = DoRefEntities[0];
        // World view
        layerE.Remove<VectorLayerView>();
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