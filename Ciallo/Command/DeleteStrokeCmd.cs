using System.Collections.Generic;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteStrokeCmd : CommandBase
{
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity strokeE) { }

    public override void Do(Entity strokeE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(strokeE);

        // Data
        strokeE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity strokeE)
    {
        // Data
        strokeE.Tag<ToSerializeTag>();
    }
}