using System;
using System.Collections.Generic;
using Arch.Core;
using Ciallo.Data;
using Godot;
using Humanizer;

namespace Ciallo.Core;

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
    public virtual World WorkingWorld { get; } = WorldManager.WorkingWorld;
    public virtual Entity Document => WorkingWorld.Document();
    public virtual string Name => GetType().Name.Humanize();
    
    /// <summary>
    /// The entities to be destroyed when this command object is deleted in the command stack.
    /// Could happen when clearing history or command beyond the limit.
    /// </summary>
    public List<Entity> DestructionQueue = [];
    
    public abstract void Do();
    public abstract void Undo();

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            DestructionQueue.ForEach(e => WorkingWorld.Destroy(e));
        }
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}