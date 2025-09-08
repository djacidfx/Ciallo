using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Godot;
using Microsoft.CodeAnalysis;

namespace Ciallo.Command;

/// <summary>
/// Inherits from UndoRedo with extra methods to manage commands.
/// </summary>
public partial class CommandManager : UndoRedo
{
    public CommandManager()
    {
        SetMaxSteps(3); // fast invoke bugs
    }

    public void AddDo(CommandWrapperObject obj)
    {
        obj.DoObject = new WrapperObject(obj.Command.DoRefEntities, obj.Command.DoRefObjects);
        AddDoMethod(new(obj, CommandWrapperObject.MethodName.Do));
        AddDoReference(obj.DoObject);
        obj.Command.DoRefObjects.ForEach(AddDoReference);
        AddDoReference(obj);
    }

    public void AddUndo(CommandWrapperObject obj)
    {
        obj.UndoObject = new WrapperObject(obj.Command.UndoRefEntities, obj.Command.UndoRefObjects);
        AddUndoMethod(new(obj, CommandWrapperObject.MethodName.Undo));
        AddUndoReference(obj.UndoObject);
        obj.Command.UndoRefObjects.ForEach(AddUndoReference);
        AddUndoReference(obj);
    }
}

public partial class CommandWrapperObject(CommandBase command) : GodotObject
{
    public CommandBase Command { get; } = command;
    public WrapperObject DoObject;
    public WrapperObject UndoObject;

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (IsInstanceValid(DoObject))
                DoObject.FreeWithoutDestroying();
            if (IsInstanceValid(UndoObject))
                UndoObject.FreeWithoutDestroying();
        }
    }

    public void Do() => Command.Do();
    public void Undo() => Command.Undo();
}

/// <summary>
/// Wrapper for the lists to be used in CommandBase.
/// Automatically destroy entities and objects when the command is deleted, unless FreeWithoutDestroying is called.
/// </summary>
/// <param name="entities">The reference to an entity List in CommandBase</param>
/// <param name="objects">The reference to an object List in CommandBase</param>
public partial class WrapperObject(List<Entity> entities, List<GodotObject> objects) : GodotObject
{
    private bool _destroy = true;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_destroy) return;
        if (entities != null && entities.Count != 0)
        {
            var world = World.Worlds.First(w => w.Id == entities.First().WorldId);
            entities.ForEach(world.Destroy);
        }

        objects?.ForEach(obj => obj.Free());
    }

    public void FreeWithoutDestroying()
    {
        _destroy = false;
        Free();
    }
}