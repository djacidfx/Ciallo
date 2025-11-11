using System;
using System.Collections.Generic;
using Frent;

namespace Ciallo.Command;

public class DeleteStrokeCmd(Entity strokeE) : CommandBase
{
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(strokeE);

    public override void Do()
    {
    }

    public override void Undo()
    {
        throw new NotImplementedException();
    }
}