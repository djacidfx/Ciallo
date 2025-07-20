using Godot;
using System;
using System.Collections.Generic;

namespace Ciallo.Core;

public partial class MenuContentFile : PopupMenu
{
    public List<string> Items =
    [
        "New",
        "Open",
        "Save",
        "Save As",
        "Close",
        "Exit"
    ];
    
    public override void _Ready()
    {
        
    }
}
