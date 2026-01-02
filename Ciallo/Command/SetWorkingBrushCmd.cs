using Ciallo.Data;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingBrushCmd : CommandBase
{
    private Entity _oldBrushE;

    protected override void BeforeFirstDo(Entity newBrushE)
    {
        _oldBrushE = Document.Get<SelectionManager>().WorkingBrush.Value;
    }

    protected override void Do(Entity newBrushE)
    {
        // Data
        var newIndex = Document.Get<BrushManager>().Brushes.IndexOf(newBrushE);
        Document.Get<SelectionManager>().WorkingBrush.Value = newBrushE;

        // UI
        var brushList = Document.Get<DocumentBrushList>();
        if (newIndex != -1)
            brushList.Select(newIndex);
        else
            brushList.DeselectAll();
    }

    protected override void Undo(Entity newBrushE)
    {
        // UI
        var brushList = Document.Get<DocumentBrushList>();
        var oldIdx = Document.Get<BrushManager>().Brushes.IndexOf(_oldBrushE);
        if (oldIdx == -1)
            brushList.DeselectAll();
        else
            brushList.Select(oldIdx);

        // Data
        Document.Get<SelectionManager>().WorkingBrush.Value = _oldBrushE;
    }
}