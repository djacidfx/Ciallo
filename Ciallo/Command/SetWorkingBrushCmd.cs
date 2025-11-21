using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

public class SetWorkingBrushCmd : CommandBase
{
    private readonly Entity _oldBrushE;
    private readonly Entity _newBrushE;

    public SetWorkingBrushCmd(Entity newBrushE)
    {
        _oldBrushE = Document.Get<SelectionManager>().WorkingBrush.Value;
        _newBrushE = newBrushE;
    }

    public override void Do()
    {
        // Data
        var newIndex = Document.Get<BrushManager>().Brushes.IndexOf(_newBrushE);
        Document.Get<SelectionManager>().WorkingBrush.Value = _newBrushE;

        // UI
        var brushList = Document.Get<DocumentBrushList>();
        if (newIndex != -1)
            brushList.Select(newIndex);
        else
            brushList.DeselectAll();
    }

    public override void Undo()
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