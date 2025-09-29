using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using ObservableCollections;
using R3;
using Arch.Core;
using Ciallo.Geometry;
using MessagePack;

namespace Ciallo.Misc;

public partial class AutoloadMisc : Node
{
    public override void _EnterTree()
    {
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        
    }

    public override void _Ready()
    {
        
    }
}