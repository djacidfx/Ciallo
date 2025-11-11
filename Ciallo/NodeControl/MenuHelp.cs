using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Godot;

namespace Ciallo.NodeControl;

public partial class MenuHelp : PopupMenu
{
    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "User manual", null },
        { "About Ciallo", null },
    };

    public override void _Ready()
    {
        foreach (var (i, item) in MenuItems.Index())
        {
            if (item.Key.StartsWith('-'))
            {
                AddSeparator();
                continue;
            }
            AddItem(Tr(item.Key));
            if (item.Value != null) SetItemShortcut(i, item.Value.Shortcut);
        }

        IndexPressed += id => OnIndexPressed((int)id);
    }

    private void OnIndexPressed(int id)
    {
        switch (id)
        {
            case 0:
                break;
            case 1:
                GetTree().GetNodesInGroup("Dialog").OfType<AcceptDialog>().First(n => n.Name == "AboutCiallo").Popup();
                break;
            case 2:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), $"Unhandled menu item index: {id}");
        }
    }
}