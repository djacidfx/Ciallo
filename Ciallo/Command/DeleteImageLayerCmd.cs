using System.Collections.Generic;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteImageLayerCmd : CommandBase
{
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity targetE) { }

    public override void Do(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }
}