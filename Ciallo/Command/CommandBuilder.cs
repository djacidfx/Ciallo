using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

public partial class CommandBuilder
{
    public readonly string ActionName = "Unnamed Action";
    public Entity TargetE;
    public readonly List<ICommand> Commands = [];

    public CommandBuilder(Entity targetE = default)
    {
        TargetE = targetE;
    }

    public CommandBuilder(string name, Entity targetE = default)
    {
        ActionName = name;
        TargetE = targetE;
    }

    public CommandBuilder SetTarget(Entity e)
    {
        TargetE = e;
        return this;
    }

    public void Commit(bool execute = true)
    {
        if (Commands.Count == 0) return;
        var cm = GetCommandManager();
        cm.Commit(ActionName, Commands, execute);
        Commands.Clear();
    }

    // Sequential value commit. Repeated calls with the same generated segment key keep
    // the original undo endpoint and replace only the redo endpoint.
    public void CommitSequence(bool execute = true)
    {
        if (Commands.Count == 0) return;
        var cm = GetCommandManager();
        cm.CommitSequence(ActionName, Commands, execute);
        Commands.Clear();
    }

    // Create one action on first commit, then keep appending later segments until
    // another history-writing entrypoint starts a different action.
    public void CommitOpenSequence(bool execute = true)
    {
        if (Commands.Count == 0) return;
        var cm = GetCommandManager();
        cm.CommitOpenSequence(ActionName, Commands, execute);
        Commands.Clear();
    }

    // Attach to latest undoable action.
    public void CommitToLatest(bool execute = true)
    {
        if (Commands.Count == 0) return;
        var cm = GetCommandManager();
        cm.CommitToLatest(ActionName, Commands, execute);
        Commands.Clear();
    }

    private CommandManager GetCommandManager()
    {
        if (TargetE.IsNull)
            TargetE = AppDocumentManager.WorkingDocument.Value;
        return TargetE.Document.Get<CommandManager>();
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
