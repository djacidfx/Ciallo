using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteShapeCmd : CommandBase
{
    public override void OnDeletedAsUndo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE) { }

    public override void Do(Entity targetE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedShapes.Remove(targetE);

        // Data
        targetE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        // Data
        targetE.Tag<ToSerializeTag>();
    }
}