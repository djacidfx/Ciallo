using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core.Extensions;
using Ciallo.Core;
using Ciallo.Data;

namespace Ciallo.NodeControl;

public partial class MenuEdit : PopupMenu
{
    // Identical to the items inside the PopupMenu node's items property.
    public static readonly Dictionary<int, StringName> IndexToActionName = new()
    {
        { 0, ActionNames.Undo },
        { 1, ActionNames.Redo },
    };
    
    public override void _Ready()
    {
        foreach(var (i, item) in IndexToActionName)
        {
            var shortcut = new Shortcut
            {
                Events = [new InputEventAction
                {
                    Action = item,
                    Pressed = true,
                }],
            };
            SetItemShortcut(i, shortcut);
        }
        
        IndexPressed += id => OnIndexPressed((int)id);
    }
    
    public static void OnIndexPressed(int id)
    {
        if(WorldManager.WorkingWorld == null) return;
        var cmdM = WorldManager.WorkingDocument.Get<CommandManager>();
        switch (id)
        {
            case 0:
            {
                GD.Print("undo");
                cmdM.Undo(); break;
            }
            case 1: cmdM.Redo(); break;
            default: GD.PrintErr("Unknown menu action"); break;
        }
    }
}
