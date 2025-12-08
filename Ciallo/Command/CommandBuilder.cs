using System.Collections.Generic;
using Frent;

namespace Ciallo.Command;

public partial class CommandBuilder
{
    public Entity TargetE;
    public List<CommandBase> Commands = [];

    public CommandBuilder(Entity targetE)
    {
        TargetE = targetE;
    }
}