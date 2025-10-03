using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Godot;

namespace Ciallo.NodeControl;

public partial class MenuFile : PopupMenu
{
    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "New document", AppActions.NewDocument },
        { "Open document", AppActions.OpenDocument },
        { "Close document", null },
        { "-1", null },
        { "Save", AppActions.Save },
        { "Save As...", AppActions.SaveAs },
        { "-2", null },
        { "Export as image", null },
        { "Export as Godot scene", null },
    };
    
    public override void _Ready()
    {
        foreach(var (i, item) in MenuItems.Index())
        {
            if(item.Key.StartsWith('-'))
            {
                AddSeparator();
                continue;
            }
            AddItem(Tr(item.Key));
            if (item.Value != null) SetItemShortcut(i, item.Value.Shortcut);
        }
        
        IndexPressed += id => OnIndexPressed((int)id);
    }
    
    public void OnIndexPressed(int id)
    {
        switch (id)
        {
            case 0: // New Document
                var dialogNew = GetTree().GetNodesInGroup("Dialog").OfType<ConfirmationDialog>().Single(n => n.Name == "NewDocument");
                dialogNew.Popup();
                break;
            case 1: // Open Document
                var dialogOpen = GetTree().GetNodesInGroup("Dialog").OfType<OpenDocument>().Single(n => n.Name == "OpenDocument");
                dialogOpen.Popup();
                break;
            case 2: // Close Document
                if(AppWorldManager.WorkingWorld.Value == null) return;
                AppWorldManager.Remove(AppWorldManager.WorkingWorld.Value);
                break;
            case 4: // Save
                if(AppWorldManager.WorkingWorld.Value == null) return;
                AppWorldManager.SaveWorkingWorld();
                break;
            default:
                GD.PrintErr($"Unhandled menu item index: {id}");
                break;
        }
    }
}
