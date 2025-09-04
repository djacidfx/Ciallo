using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ChangeWorkingLayerCmd : CommandBase
{
    private readonly List<int> _newSelectedPath;
    private List<int> _oldSelectedPath;
    private Entity _oldSelectedLayerE;
    
    public ChangeWorkingLayerCmd(IReadOnlyList<int> path)
    {
        _newSelectedPath = path.ToList();
        var m = Document.Get<LayerTreeManager>();
    }

    public override void Do()
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        var tree = Document.Get<LayerTreeManager>();
        _oldSelectedLayerE = sm.WorkingLayer.Value;
        _oldSelectedPath = _oldSelectedLayerE != Entity.Null ? tree.Root.GetPathTo(sm.WorkingLayer.Value) : null;
        sm.WorkingLayer.Value = tree.Root.GetEntity(_newSelectedPath);
        
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(_newSelectedPath);
    }

    public override void Undo()
    {
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(_oldSelectedPath);
        
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = _oldSelectedLayerE;
    }
}