using System;
using Ciallo.Geometry;
using Ciallo.Misc;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using MessagePackGodot;

namespace Ciallo.Data;

public partial class DataAutoload : Node
{
    public IFormatterResolver DefaultResolver;
    
    public override void _Ready()
    {
        DefaultResolver = CompositeResolver.Create(
            GodotResolver.Instance,
            AttributeFormatterResolver.Instance,
            ReactivePropertyResolver.Instance,
            StandardResolver.Instance
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(DefaultResolver);
        
        Preferences.Load();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        WorldManager.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
            
        Preferences.Save();
    }
}