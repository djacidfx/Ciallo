using System;
using Frent;

namespace Ciallo.Command;

public class DeleteFillMarkerCmd : CommandBase
{
    public override void OnDeletedAsUndo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        throw new NotImplementedException();
    }

    public override void Do(Entity targetE)
    {
        throw new NotImplementedException();
    }

    public override void Undo(Entity targetE)
    {
        throw new NotImplementedException();
    }
}