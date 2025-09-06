using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

/// <summary>
/// Wrapper for Entity list to be used in CommandBase.
/// Automatically destroy entities when the command is deleted, unless FreeWithoutDestroyingEntities is called.
/// </summary>
/// <param name="entities">Referencing existing entity list object.</param>
public partial class EntityWrapperObject(List<Entity> entities) : GodotObject
{
    private bool _destroyEntities = true;
    
    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_destroyEntities) return;
        if (entities == null || entities.Count == 0) return;
        var world = WorldManager.GetWorldById(entities.First().WorldId);
        entities.ForEach(world.Destroy);
    }
    
    public void FreeWithoutDestroyingEntities()
    {
        _destroyEntities = false;
        Free();
    }
}

public partial class CommandWrapperObject(CommandBase command) : GodotObject
{
    public CommandBase Command { get; } = command;
    public EntityWrapperObject DoEntityObject;
    public EntityWrapperObject UndoEntityObject;

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (IsInstanceValid(DoEntityObject))
                DoEntityObject.FreeWithoutDestroyingEntities();
            if (IsInstanceValid(UndoEntityObject))
                UndoEntityObject.FreeWithoutDestroyingEntities();
        }
    }

    public void Do() => Command.Do();
    public void Undo() => Command.Undo();
}