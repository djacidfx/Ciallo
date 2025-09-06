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
    public World WorkingWorld { get; set; } = WorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public virtual string Name => GetType().Name.Humanize();

    public Array<Node> GetNodesInGroup(StringName group) => ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup(group);

    /// <summary>
    /// `DoRefEntities` are the entities will be destroyed when this command object is ready to call redo() and deleted.
    /// e.g. User undo the most recent command, then clear the whole history. So the most recent command satisfies the above statement.
    /// Entity version of `add_do_reference`.
    /// </summary>
    public readonly List<Entity> DoRefEntities = [];
    private EntityGodotObject _doEntityGodotObject;
    public readonly List<Entity> UndoRefEntities = [];
    private EntityGodotObject _undoEntityGodotObject;

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
        foreach (var obj in objects)
        {
            cm.AddDoReference(obj);
            _doEntityGodotObject = new EntityGodotObject(DoRefEntities);
            cm.AddDoReference(_doEntityGodotObject);
            cm.AddDoMethod(new(obj, CommandWrapperObject.MethodName.Do));
        }
        foreach (var obj in objects.Reverse())
        {
            cm.AddUndoMethod(new(obj, CommandWrapperObject.MethodName.Undo));
            _undoEntityGodotObject = new EntityGodotObject(UndoRefEntities);
            cm.AddUndoReference(_undoEntityGodotObject);
            cm.AddUndoReference(obj);
        }
        cm.CommitAction(execute);
    }

    private readonly List<CommandBase> _combinations = [];
    public CommandBase Combine(CommandBase other)
    {
        _combinations.Add(other);
        return this;
    }

    public void FreeGodotObject()
    {
        if(GodotObject.IsInstanceValid(_doEntityGodotObject))
            _doEntityGodotObject.FreeWithoutDestroyingEntities();
        if(GodotObject.IsInstanceValid(_undoEntityGodotObject))
            _undoEntityGodotObject.FreeWithoutDestroyingEntities();
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}