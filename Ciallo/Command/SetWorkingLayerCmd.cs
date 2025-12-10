using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingLayerCmd : CommandBase
{
    public Entity OldLayerE;

    public override void Do(Entity newLayerE)
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        if (OldLayerE.IsNull) OldLayerE = sm.WorkingLayer.Value;
        sm.WorkingLayer.Value = newLayerE;

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(newLayerE);
    }

    public override void Undo(Entity newLayerE)
    {
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(OldLayerE);

        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = OldLayerE;
    }
}