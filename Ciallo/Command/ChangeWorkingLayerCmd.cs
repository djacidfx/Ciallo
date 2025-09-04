using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ChangeWorkingLayerCmd : CommandBase
{
    private readonly ImmutableArray<int> _newPath;
    private ImmutableArray<int> _oldPath;
    
    public ChangeWorkingLayerCmd(IReadOnlyList<int> newPath)
    {
        _newPath = [..newPath];
    }

    public override void Do()
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        _oldPath = sm.WorkingLayerPath;
        sm.WorkingLayer = Document.Get<LayerTreeManager>().Root.GetDescendant(_newPath);
        
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(_newPath);
    }

    public override void Undo()
    {
        // Layer tree view
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(_oldPath);
        
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer = Document.Get<LayerTreeManager>().Root.GetDescendant(_oldPath);
    }
}