using Ciallo.Data;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingStrokeBrushCmd : CommandBase
{
    private Entity _oldBrushE;

    public override void BeforeFirstDo(Entity newBrushE)
    {
        _oldBrushE = Document.Get<SelectionManager>().WorkingStrokeBrush.Value;
    }

    public override void Do(Entity newBrushE)
    {
        // Data
        var newIndex = Document.Get<BrushManager>().StrokeBrushEs.IndexOf(newBrushE);
        Document.Get<SelectionManager>().WorkingStrokeBrush.Value = newBrushE;

        // UI
        var brushList = Document.Get<DocumentBrushListViewer>();
        if (newIndex != -1)
            brushList.Select(newIndex);
        else
            brushList.DeselectAll();
    }

    public override void Undo(Entity newBrushE)
    {
        // UI
        var brushList = Document.Get<DocumentBrushListViewer>();
        var oldIdx = Document.Get<BrushManager>().StrokeBrushEs.IndexOf(_oldBrushE);
        if (oldIdx == -1)
            brushList.DeselectAll();
        else
            brushList.Select(oldIdx);

        // Data
        Document.Get<SelectionManager>().WorkingStrokeBrush.Value = _oldBrushE;
    }
}