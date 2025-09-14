using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;
using Godot.Collections;
using Humanizer;

namespace Ciallo.Command;

/// <remarks>
/// Shen: Several attempts have been made to allow `using var cmd = new Cmd()`, but all failed.
/// This class cannot be `RefCounted` otherwise cannot get notification of Predelete.
/// Override the Dispose method get unknown/undefined behavior.
/// </remarks>>
/// <summary>
/// `using var cmd = new Cmd()` cause memory leak. Must manually call `cmd.Free()`.
/// </summary>
public abstract class CommandBase
{
    /// <summary>
    /// The world in which this command operates.
    /// </summary>
    public World WorkingWorld { get; set; } = AppWorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public virtual string Name => GetType().Name.Humanize();
    public SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();

    public Array<Node> GetNodesInGroup(StringName group) => SceneTree.GetNodesInGroup(group);

    /// <summary>
    /// `DoRefEntities` are the entities will be destroyed when this command object is ready to call redo() and deleted.
    /// e.g. User undo the most recent command, then clear the whole history. So the most recent command satisfies the above statement.
    /// Entity version of `add_do_reference`.
    /// </summary>
    public virtual IEnumerable<Entity> DoRefEntities => null;
    public virtual IEnumerable<Entity> UndoRefEntities => null;

    public virtual IEnumerable<GodotObject> DoRefObjects => null;
    public virtual IEnumerable<GodotObject> UndoRefObjects => null;

    public abstract void Do();
    public abstract void Undo();

    
    public void Commit(bool execute = true)
    {
        if(WorkingWorld == null)
        {
            GD.PushWarning("WorkingWorld is null");
            return;
        };
        var cm = WorkingWorld.Document().Get<CommandManager>();
        
        // Add Do/Undo Reference method order matters:
        CommandBase[] commands =  [this, .._combinations];
        var objects = commands.Select(c => new CommandWrapperObject(c)).ToArray();
        
        cm.CreateAction(Name);
        foreach (var obj in objects) cm.AddDo(obj);
        foreach (var obj in objects.Reverse()) cm.AddUndo(obj);
        cm.CommitAction(execute);
    }

    private readonly List<CommandBase> _combinations = [];
    public CommandBase Combine(CommandBase other)
    {
        _combinations.Add(other);
        return this;
    }

    public override string ToString()
    {
        return $"{Name}";
    }
    
    public static IEnumerable<Entity> ToEnumerable(Entity value)
    {
        if (value != Entity.Null) yield return value;
    }

}