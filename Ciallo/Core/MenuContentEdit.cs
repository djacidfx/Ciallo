using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ciallo.Core;

public partial class MenuContentEdit : PopupMenu
{
    public readonly List<StringName> Items =
    [
        ActionNames.Undo,
        ActionNames.Redo,
    ];

    public override void _Ready()
    {
        foreach (var (i, item) in Items.Index())
        {
            AddItem(item, i);
        }
        
        foreach(var (i, item) in Items.Index())
        {
            var action = new InputEventAction
            {
                Action = item,
                Pressed = true,
            };
            var shortcut = new Shortcut
            {
                Events = [action]
            };
            SetItemShortcut(i, shortcut, true);
        }
    }
    
    public void OnIndexPressed(int id)
    {
        GD.Print(Items[id]);
    }
}
