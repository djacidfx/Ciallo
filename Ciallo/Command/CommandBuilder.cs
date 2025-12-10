using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

public partial class CommandBuilder
{
    public readonly string Name = "Unnamed Action";
    public Entity TargetE;
    public readonly List<CommandBase> Commands = [];

    public CommandBuilder(Entity targetE = default)
    {
        TargetE = targetE;
    }

    public CommandBuilder(string name, Entity targetE)
    {
        Name = name;
        TargetE = targetE;
    }

    public CommandBuilder SetTarget(Entity e)
    {
        TargetE = e;
        return this;
    }

    public void AddCommand(CommandBase cmd)
    {
        Commands.Add(cmd);
    }

    public void Commit(bool execute = true)
    {
        if (Commands.Count == 0) return;
        var cm = TargetE.World.Document().Get<CommandManager>();

        // Add Do/Undo Reference methods, order matters:
        var objects = Commands.Select(c => new CommandWrapperObject(c)).ToArray();

        cm.CreateAction(Name);
        foreach (var obj in objects) cm.AddDo(obj);
        foreach (var obj in objects.AsEnumerable().Reverse()) cm.AddUndo(obj);
        cm.CommitAction(execute);
    }

    public void Do()
    {
        foreach (var cmd in Commands)
        {
            cmd.Do();
        }
    }

    public void Undo()
    {
        foreach (var cmd in Commands.AsEnumerable().Reverse())
        {
            cmd.Undo();
        }
    }
}