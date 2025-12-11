using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingLayerCmd : CommandBase
{
    public Entity OldLayerE;

    protected override void BeforeFirstDo(Entity newLayerE)
    {
        var sm = Document.Get<SelectionManager>();
        OldLayerE = sm.WorkingLayer.Value;
    }

    protected override void Do(Entity newLayerE)
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = newLayerE;

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(newLayerE);
    }

    protected override void Undo(Entity newLayerE)
    {
        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.SetWorkingLayerNoSignal(OldLayerE);

        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = OldLayerE;
    }
}