using System;
using System.Collections.Generic;
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
public abstract partial class CommandBase : GodotObject
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
    public EntityGodotObject DoEntityGodotObject;
    public readonly List<Entity> UndoRefEntities = [];
    public EntityGodotObject UndoEntityGodotObject;

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
        cm.CreateAction(Name);
        DoEntityGodotObject = new EntityGodotObject(DoRefEntities);
        cm.AddDoReference(DoEntityGodotObject);
        cm.AddDoReference(this);
        
        cm.AddDoMethod(new(this, MethodName.Do));
        cm.AddUndoMethod(new(this, MethodName.Undo));
        
        UndoEntityGodotObject = new EntityGodotObject(UndoRefEntities);
        cm.AddUndoReference(UndoEntityGodotObject);
        cm.AddUndoReference(this);
        cm.CommitAction(execute);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if(IsInstanceValid(DoEntityGodotObject))
                DoEntityGodotObject.FreeWithoutDestroyingEntities();
            if(IsInstanceValid(UndoEntityGodotObject))
                UndoEntityGodotObject.FreeWithoutDestroyingEntities();
        }
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}