using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Core;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Consider a document as a special singleton entity of the world.
/// All the "document-level singleton data" should be stored in the singleton entity. (Program-level singleton we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as the document settings, layer tree, etc.
/// </summary>
public static class WorldManager
{
    private static readonly List<World> LoadedWorld = [];
    // Current focused document.
    public static readonly ReactiveProperty<World> WorkingWorld = new(null);
    public static Entity WorkingDocument => WorkingWorld.Value.Singleton();
    private static readonly Dictionary<World, Entity> DocumentSingletons = [];

    public static World Create([NotNull] DocumentSetting settings)
    {
        var world = World.Create();
        world.AddForbiddenComponents();
        // Only one loaded world is supported for current version.
        Clear();
        LoadedWorld.Add(world);
        WorkingWorld.Value = world;

        // Init empty document
        var document = world.Create();
        DocumentSingletons.Add(world, document);

        var layerTreeManager = new LayerTreeManager();
        var selectionManager = new SelectionManager();
        var commandManager = new CommandManager();
        document.Add(settings, layerTreeManager, selectionManager, commandManager);
        
        return world;
    }

    public static void Remove(World world)
    {
        if (!LoadedWorld.Contains(world)) throw new KeyNotFoundException("The specified world does not exist in the document manager.");
        LoadedWorld.Remove(world);
        world.Dispose();
        DocumentSingletons.Remove(world);
    }
    
    public static void Clear()
    {
        foreach (var world in LoadedWorld)
        {
            world.Dispose();
        }
        DocumentSingletons.Clear();
        LoadedWorld.Clear();
    }

    public static Entity Singleton(this World world)
    {
        return DocumentSingletons[world];
    }

    public static void AddForbiddenComponents(this World world)
    {
        var throwError = new Action(() => throw new InvalidOperationException("Primitive types cannot be used as components."));
        world.SubscribeComponentAdded((in Entity e, ref Entity _)  => throwError()); // The most common mistake
        world.SubscribeComponentAdded((in Entity e, ref int _)=> throwError());
        world.SubscribeComponentAdded((in Entity e, ref float _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref double _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref bool _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref string _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref char _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref Vector2 _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref Vector2I _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref Transform2D _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref Rect2 _) => throwError());
        world.SubscribeComponentAdded((in Entity e, ref Rect2I _) => throwError());
        var throwCollectionError = new Action(() => throw new InvalidOperationException("List of primitive types cannot be used as components."));
        world.SubscribeComponentAdded((in Entity e, ref List<Entity> _)  => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<int> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<float> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<double> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<bool> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<string> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<char> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<Vector2> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<Vector2I> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<Transform2D> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<Rect2> _) => throwCollectionError());
        world.SubscribeComponentAdded((in Entity e, ref List<Rect2I> _) => throwCollectionError());
    }
}