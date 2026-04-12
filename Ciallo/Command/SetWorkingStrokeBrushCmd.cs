using Ciallo.Data;
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
        Document.Get<SelectionManager>().WorkingStrokeBrush.Value = newBrushE;
    }

    public override void Undo(Entity newBrushE)
    {
        Document.Get<SelectionManager>().WorkingStrokeBrush.Value = _oldBrushE;
    }
}