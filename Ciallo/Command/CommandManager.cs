using System;
using System.Collections.Generic;
using Arch.Core;
using Godot;

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
}