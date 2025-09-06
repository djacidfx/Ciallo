using System;
using System.Collections.Generic;
using Arch.Core;
using Godot;
using Microsoft.CodeAnalysis;

namespace Ciallo.Command;

// Shen: Has been tortured several hours by Godot's object management system.
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
        obj.DoEntityObject = new EntityWrapperObject(obj.Command.DoRefEntities);
        AddDoMethod(new(obj, CommandWrapperObject.MethodName.Do));
        AddDoReference(obj.DoEntityObject);
        obj.Command.DoRefObjects.ForEach(AddDoReference);
        AddDoReference(obj);
    }
    
    public void AddUndo(CommandWrapperObject obj)
    {
        obj.UndoEntityObject = new EntityWrapperObject(obj.Command.UndoRefEntities);
        AddUndoMethod(new(obj, CommandWrapperObject.MethodName.Undo));
        AddUndoReference(obj.UndoEntityObject);
        obj.Command.UndoRefObjects.ForEach(AddUndoReference);
        AddUndoReference(obj);
    }
}