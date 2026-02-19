using System;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillLayerCmd : CommandBase
{
    public override void BeforeFirstDo(Entity targetE) { }

    public override void Do(Entity targetE)
    {
        throw new NotImplementedException();
    }
    public override void Undo(Entity targetE)
    {
        throw new NotImplementedException();
    }
}