using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
public class ChangeWorkingLayerCmd : CommandBase
{
    public Entity NewE;
    public Entity OldE = Entity.Null;

    public ChangeWorkingLayerCmd(Index index)
    {
        NewE = Document.Get<LayerTreeManager>().Root.GetChild(index);
    }

    public ChangeWorkingLayerCmd(Entity layerE)
    {
        NewE = layerE;
    }

    public override void Do()
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        OldE = sm.WorkingLayer.Value;
        sm.WorkingLayer.Value = NewE;
        
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(NewE);
    }

    public override void Undo()
    {
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(OldE);
        
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = OldE;
    }
}