using System;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

public class SetWorkingLayerCmd : CommandBase
{
    public Index NewIndex = int.MaxValue;
    public Entity NewE;
    public Entity OldE;

    public SetWorkingLayerCmd(Index index)
    {
        NewIndex = index;
    }

    public SetWorkingLayerCmd(Entity layerE)
    {
        NewE = layerE;
    }

    public override void Do()
    {
        if (NewE.IsNull && NewIndex.Value != int.MaxValue)
            NewE = Document.Get<LayerTreeNode>().GetChild(NewIndex);
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