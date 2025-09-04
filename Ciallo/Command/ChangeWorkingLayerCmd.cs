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
    private readonly ImmutableArray<int> _newSelectedPath;
    private ImmutableArray<int> _oldSelectedPath;
    
    public ChangeWorkingLayerCmd(ImmutableArray<int> path)
    {
        _newSelectedPath = path;
    }

    public override void Do()
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        if(sm.WorkingLayerPath != null)
            _oldSelectedPath = [..sm.WorkingLayerPath];
        sm.WorkingLayerPath = _newSelectedPath;
        
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
        sm.WorkingLayerPath = _oldSelectedPath;
    }
}