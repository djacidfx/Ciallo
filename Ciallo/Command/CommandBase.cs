using System.Collections.Generic;
using Ciallo.Data;
using Frent;
using Godot;
using Godot.Collections;
using Humanizer;

namespace Ciallo.Command;

public abstract class CommandBase : ICommand
{
    public bool IsExecuted;
    public Entity TargetE { protected get; set; }
    public World WorkingWorld => TargetE.World;
    public Entity Document => WorkingWorld.Document();
    public virtual string Name => GetType().Name.Humanize();
    public SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();

    public Array<Node> GetNodesInGroup(StringName group) => SceneTree.GetNodesInGroup(group);

    protected CommandBase(Entity targetE = default)
    {
        TargetE = targetE;
    }

    /// <summary>
    /// `DoRefEntities` are the entities will be destroyed when this command is ready to redo and deleted.
    /// e.g. User undo the most recent command, then clear the whole history. So the most recent command satisfies the above statement.
    /// Entity version of `add_do_reference`.
    /// </summary>
    public virtual IEnumerable<Entity> DoRefEntities => [];
    public virtual IEnumerable<Entity> UndoRefEntities => [];

    public virtual IEnumerable<GodotObject> DoRefObjects => [];
    public virtual IEnumerable<GodotObject> UndoRefObjects => [];

    public virtual void BeforeFirstDo(Entity targetE) { }
    public abstract void Do(Entity targetE);
    public abstract void Undo(Entity targetE);

    public void Do()
    {
        if (!IsExecuted) BeforeFirstDo(TargetE);
        Do(TargetE);
        IsExecuted = true;
    }

    public void Undo() => Undo(TargetE);

    public void Commit(bool execute = true)
    {
        var cm = Document.Get<CommandManager>();

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