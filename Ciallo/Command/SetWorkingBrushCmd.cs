using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingBrushCmd : CommandBase
{
    private Entity _oldBrushE;
    private readonly Entity _newBrushE;

    public SetWorkingBrushCmd(Entity newBrushE)
    {
        _newBrushE = newBrushE;

        // Dirty hack
        AppBrushLibrary.SelectedIndex.Value = -1;
    }

    public override void Do(Entity document)
    {
        if (_oldBrushE.IsNull) _oldBrushE = document.Get<SelectionManager>().WorkingBrush.Value;
        // Data
        var newIndex = document.Get<BrushManager>().Brushes.IndexOf(_newBrushE);
        document.Get<SelectionManager>().WorkingBrush.Value = _newBrushE;

        // UI
        var brushList = document.Get<DocumentBrushList>();
        if (newIndex != -1)
            brushList.Select(newIndex);
        else
            brushList.DeselectAll();
    }

    public override void Undo(Entity document)
    {
        // UI
        var brushList = document.Get<DocumentBrushList>();
        var oldIdx = document.Get<BrushManager>().Brushes.IndexOf(_oldBrushE);
        if (oldIdx == -1)
            brushList.DeselectAll();
        else
            brushList.Select(oldIdx);

        // Data
        document.Get<SelectionManager>().WorkingBrush.Value = _oldBrushE;
    }
}