using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using ObservableCollections;
using R3;
using Arch.Core;
using MessagePack;

namespace Ciallo.Misc;

public partial class Autoload : Node
{
    public override void _EnterTree()
    {
        
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            ProgramPreferences.Save();
    }

    public override void _Ready()
    {
        
    }
    
    public static IEnumerable<Type> GetSerializableTypes()
    {
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
        {
            try
            {
                return a.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }).Where(t => t is { IsAbstract: false });
        
        return allTypes.Where(t => t!.GetCustomAttributes(typeof(MessagePackObjectAttribute), false).Length > 0);
    }
}