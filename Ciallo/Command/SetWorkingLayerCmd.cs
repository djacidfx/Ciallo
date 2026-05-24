using Ciallo.Data;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingLayerCmd : CommandBase
{
    public Entity OldLayerE;

    public override void BeforeFirstDo(Entity newLayerE)
    {
        var sm = Document.Get<SelectionManager>();
        OldLayerE = sm.WorkingLayer.Value;
    }

    public override void Do(Entity newLayerE)
    {
        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = newLayerE;

        // Layer panel
        var layerTree = Document.Get<LayerTree>();
        layerTree.SetWorkingLayerNoSignal(newLayerE);

        // Timeline panel
        var trackTree = Document.Get<TrackTree>();
        trackTree.SetWorkingLayerNoSignal(newLayerE);
    }

    public override void Undo(Entity newLayerE)
    {
        var trackTree = Document.Get<TrackTree>();
        trackTree.SetWorkingLayerNoSignal(OldLayerE);

        // Layer panel
        var layerTree = Document.Get<LayerTree>();
        layerTree.SetWorkingLayerNoSignal(OldLayerE);

        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = OldLayerE;
    }
}