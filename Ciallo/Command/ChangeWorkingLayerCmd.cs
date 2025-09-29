using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public class ChangeWorkingLayerCmd(IReadOnlyList<int> newPath) : CommandBase
{
    private readonly ImmutableArray<int> _newPath = [..newPath];
    private ImmutableArray<int>? _oldPath;
    
    public override void Do()
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        _oldPath ??= sm.WorkingLayerPath;
        sm.WorkingLayer.Value = _newPath.Length > 0 ? Document.Get<LayerTreeManager>().Root.GetDescendant(_newPath) : Entity.Null;
        
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
        sm.WorkingLayer.Value = _oldPath!.Value.Length > 0 ? Document.Get<LayerTreeManager>().Root.GetDescendant(_oldPath) : Entity.Null;
    }
}