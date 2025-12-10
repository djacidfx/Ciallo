using System.Collections.Generic;
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
    public void Undo() => Undo(TargetE);

    public void Commit(bool execute = true)
    {
        var cm = WorkingWorld.Document().Get<CommandManager>();

        // Add Do/Undo Reference methods, order matters:
        var obj = new CommandWrapperObject(this);

        cm.CreateAction(Name);
        cm.AddDo(obj);
        cm.AddUndo(obj);
        cm.CommitAction(execute);
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