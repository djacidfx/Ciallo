using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Godot;

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

    public CommandBuilder(string name, Entity targetE)
    {
        ActionName = name;
        TargetE = targetE;
    }

    public CommandBuilder SetTarget(Entity e)
    {
        TargetE = e;
        return this;
    }

    public void Commit(bool execute = true, MergeMode mergeMode = MergeMode.Disable)
    {
        if (Commands.Count == 0) return;
        if (TargetE.IsNull) throw new InvalidOperationException("TargetE is not set in CommandBuilder.");
        var document = TargetE.Document;
        var cm = document.Get<CommandManager>();

        // Add Do/Undo Reference methods, order matters:
        var objects = Commands.Select(c => new CommandWrapperObject(c)).ToArray();

        UndoRedo.MergeMode mode = mergeMode switch
        {
            MergeMode.Disable => UndoRedo.MergeMode.Disable,
            MergeMode.ForceMergeLatest => UndoRedo.MergeMode.All,
            MergeMode.MergeEnd => UndoRedo.MergeMode.Ends,
            _ => throw new ArgumentOutOfRangeException(nameof(mergeMode), mergeMode, null)
        };

        string actionName = ActionName;
        if (mergeMode == MergeMode.ForceMergeLatest)
        {
            string name = cm.GetCurrentActionName();
            if (!string.IsNullOrEmpty(name))
                actionName = name;
        }

        cm.CreateAction(actionName, mode);
        foreach (var obj in objects) cm.AddDo(obj);
        foreach (var obj in objects.AsEnumerable().Reverse()) cm.AddUndo(obj);
        cm.CommitAction(execute);

        Commands.Clear();
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
