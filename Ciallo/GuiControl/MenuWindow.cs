using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Godot;

namespace Ciallo.GuiControl;

public partial class MenuWindow : PopupMenu
{
    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "Brush library", null },
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
                GetTree().GetNodesInGroup("Dialog").OfType<BrushPanel>().First().Popup();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), $"Unhandled menu item index: {id}");
        }
    }
}