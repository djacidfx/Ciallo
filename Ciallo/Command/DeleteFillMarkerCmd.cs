using System;
using System.Collections.Generic;
using Frent;

namespace Ciallo.Command;

public class DeleteFillMarkerCmd : CommandBase
{
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void Do(Entity targetE)
    {
        throw new NotImplementedException();
    }

    public override void Undo(Entity targetE)
    {
        throw new NotImplementedException();
    }
}