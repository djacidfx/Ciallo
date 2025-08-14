using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Core;

namespace Ciallo.Widget;

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
            case 0: // New Document
                var dialogNew = GetTree().GetNodesInGroup("Dialogs").OfType<ConfirmationDialog>().Single(n => n.Name == "NewDocument");
                dialogNew.Popup();
                break;
            case 1: // Open Document
                var dialogOpen = GetTree().GetNodesInGroup("Dialogs").OfType<FileDialog>().Single(n => n.Name == "OpenDocument");
                dialogOpen.Popup();
                break;
            case 3: // Save
                
                break;
            default:
                GD.PrintErr($"Unhandled menu item index: {id}");
                break;
        }
    }
}
