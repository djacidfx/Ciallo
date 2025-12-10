using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Godot;
using Godot.Collections;
using Humanizer;

namespace Ciallo.Command;

public abstract class CommandBase
{
    public Entity TargetE { protected get; init; }
    public World WorkingWorld => TargetE.World;
    public Entity Document => WorkingWorld.Document();
    public virtual string Name => GetType().Name.Humanize();
    public SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();
    public CommandManager CommandManager => Document.Get<CommandManager>();

    public Array<Node> GetNodesInGroup(StringName group) => SceneTree.GetNodesInGroup(group);

    /// <summary>
    /// `DoRefEntities` are the entities will be destroyed when this command is ready to redo and deleted.
    /// e.g. User undo the most recent command, then clear the whole history. So the most recent command satisfies the above statement.
    /// Entity version of `add_do_reference`.
    /// </summary>
    public virtual IEnumerable<Entity> DoRefEntities => null;
    public virtual IEnumerable<Entity> UndoRefEntities => null;

    public virtual IEnumerable<GodotObject> DoRefObjects => null;
    public virtual IEnumerable<GodotObject> UndoRefObjects => null;

    public abstract void Do(Entity targetE);
    public abstract void Undo(Entity targetE);

    public void Do() => Do(TargetE);
    public void Undo() => Do(TargetE);

    public void Commit(bool execute = true)
    {
        if (WorkingWorld == null)
        {
            GD.PushWarning("WorkingWorld is null");
            return;
        }

        var cm = WorkingWorld.Document().Get<CommandManager>();

        // Add Do/Undo Reference methods, order matters:
        var commands = GetDepthFirstCommands().ToArray();
        var objects = commands.Select(c => new CommandWrapperObject(c)).ToArray();

        cm.CreateAction(Name);
        foreach (var obj in objects) cm.AddDo(obj);
        foreach (var obj in objects.AsEnumerable().Reverse()) cm.AddUndo(obj);
        cm.CommitAction(execute);
    }

    public void DoAllCombination(bool useRootWorld = true)
    {
        foreach (var cmd in GetDepthFirstCommands())
        {
            cmd.Do();
        }
    }

    public void UndoAllCombination(bool useRootWorld = true)
    {
        foreach (var cmd in GetDepthFirstCommands().Reverse())
        {
            cmd.Undo();
        }
    }

    private readonly List<CommandBase> _combinations = [];

    public CommandBase Combine(CommandBase other)
    {
        _combinations.Add(other);
        return this;
    }

    // Recursively yields this command and all combined commands in depth-first order
    private IEnumerable<CommandBase> GetDepthFirstCommands()
    {
        yield return this;
        foreach (var cmd in _combinations)
        foreach (var subCmd in cmd.GetDepthFirstCommands())
            yield return subCmd;
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public static IEnumerable<Entity> ToEnumerable(Entity value)
    {
        if (!value.IsNull) yield return value;
    }
}