using Godot;
using System;
using System.Collections.Generic;
using Ciallo.Core;

namespace Ciallo.Gui;

public partial class MenuContentFile : PopupMenu
{
    // Identical to the items inside the PopupMenu node's items property.
    public static readonly Dictionary<int, StringName> IndexToActionName = new()
    {
        { 0, ActionNames.NewDocument },
        { 1, ActionNames.OpenDocument },
        { 3, ActionNames.Save },
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
    
    public void OnIndexPressed(int id)
    {
        switch (id)
        {
            
        }
    }
}
