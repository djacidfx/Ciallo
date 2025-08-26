using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Arch.Core;
using Arch.Core.Extensions;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Consider a document as a special singleton entity of the world.
/// All the "document-level singleton data" should be stored in the singleton entity. (Program-level singleton we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as the document settings, layer tree, etc.
/// </summary>
public static class DocumentManager
{
    private static readonly List<World> LoadedDocuments = [];
    // Current focused document.
    public static readonly ReactiveProperty<World> ActiveWorld = new(null);
    public static Entity ActiveDocument => ActiveWorld.Value.Singleton();
    private static readonly Dictionary<World, Entity> DocumentSingletons = [];

    public static World CreateDocument([NotNull] DocumentSetting settings)
    {
        var world = World.Create();
        world.AddForbiddenComponents();
        // Only one document supported in the view layer for current version.
        ClearDocuments();
        LoadedDocuments.Add(world);
        ActiveWorld.Value = world;

        // Init empty document
        var singleton = world.Create();
        DocumentSingletons.Add(world, singleton);
        singleton.Add(settings);
        
        var layerTreeManager = new LayerTreeManager()
        {
            World = world,
        };
        var layerE = layerTreeManager.CreateAddVectorLayer();
        singleton.Add(layerTreeManager);
        
        var selectionManager = new SelectionManager
        {
            ActiveLayer = { Value = layerE }
        };
        singleton.Add(selectionManager);
        
        return world;
    }

    public static void RemoveDocument(World world)
    {
        if (!LoadedDocuments.Contains(world)) throw new KeyNotFoundException("The specified world does not exist in the document manager.");
        LoadedDocuments.Remove(world);
        world.Dispose();
        DocumentSingletons.Remove(world);
    }
    
    public static void ClearDocuments()
    {
        foreach (var world in LoadedDocuments)
        {
            world.Dispose();
        }
        DocumentSingletons.Clear();
        LoadedDocuments.Clear();
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