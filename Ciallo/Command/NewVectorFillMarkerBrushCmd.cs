using System;
using System.Collections.Generic;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillMarkerBrushCmd : CommandBase
{
    public Entity CopyE { get; }

    public NewVectorFillMarkerBrushCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

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