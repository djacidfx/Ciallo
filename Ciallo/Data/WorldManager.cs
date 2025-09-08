using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// We consider world and document has one-to-one relationship.
/// In practice, a document is a special singleton entity of the world.
/// All the "document-level singleton data" should be stored in this "document" entity. (Program-level singleton we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as the DocumentSetting, LayerTree, etc.
/// </summary>
public static class WorldManager
{
    public static readonly List<World> LoadedWorlds = [];
    // Current focused document.
    public static readonly ReactiveProperty<World> WorkingWorld = new(null);
    public static Entity WorkingDocument => WorkingWorld.Value.Document();
    
    private static readonly Dictionary<World, Entity> DocumentSingletons = [];
    private static readonly SceneTree SceneTree = (SceneTree)Engine.GetMainLoop();

    public static World Create([NotNull] DocumentSetting settings)
    {
        // Only one loaded world is supported for current version.
        Clear();
        var world = World.Create();
        world.AddForbiddenComponents();

        // Init empty document
        var document = world.Create();
        DocumentSingletons.Add(world, document);

        // Add managers
        var layerTreeManager = new LayerTreeManager();
        var selectionManager = new SelectionManager();
        var commandManager = new CommandManager();
        document.Add(settings, layerTreeManager, selectionManager, commandManager);
        
        // Create layer tree control
        var layerPanel = SceneTree.GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
        layerPanel.CreateAddLayerContainer(document);
        
        // Create paint panel
        var paintPanelContainer = SceneTree.GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
        var paintPanel = paintPanelContainer.CreateAddPaintPanel(document);
        
        // Add world view
        var worldView = paintPanel.GetNode<WorldView>("%WorldView");
        document.Add(worldView);
        
        // Set as working world
        WorkingWorld.Value = world;
        LoadedWorlds.Add(world);
        
        // Add initial layer
        var c = new NewStrokeLayerCmd([0]);
        c.Do();
        var s = new ChangeWorkingLayerCmd([0]);
        s.Do();
        
        return world;
    }

    public static void Remove([NotNull] World world)
    {
        if (!LoadedWorlds.Contains(world)) throw new KeyNotFoundException("The specified world does not exist.");
        
        // Remove working world
        LoadedWorlds.Remove(world);
        if(WorkingWorld.Value == world) WorkingWorld.Value = LoadedWorlds.Count > 0 ? LoadedWorlds[0] : null;
        
        // Remove work view
        var worldView = world.Document().Get<WorldView>();
        world.Document().Remove<WorldView>();
        if(GodotObject.IsInstanceValid(worldView)) worldView.Free();
        
        // Remove paint panel
        //// add null check since the method could be called as long as Godot cleaning up nodes.
        var paintPanelContainer = SceneTree.GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().SingleOrDefault();
        if(GodotObject.IsInstanceValid(paintPanelContainer)) paintPanelContainer.RemoveFreePaintPanel(world.Document());
        
        // Remove layer tree control
        var layerPanel = SceneTree.GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().SingleOrDefault();
        layerPanel?.RemoveFreeLayerContainer(world.Document());

        // Dispose or free managers
        world.Document().Get<CommandManager>().Free();
        
        // Dispose world
        DocumentSingletons.Remove(world);
        world.Dispose();
    }
    
    public static void Clear()
    {
        foreach (var world in LoadedWorlds.ToList())
        {
            Remove(world);
        }
    }

    public static Entity Document([NotNull] this World world)
    {
        return DocumentSingletons[world];
    }

    public static void AddForbiddenComponents(this World world)
    {
        var throwError = new Action(() => throw new InvalidOperationException("Primitive types cannot be used as components."));
        // ReSharper disable UnusedParameter.Local
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
    
    public static World GetWorldById(int id) => LoadedWorlds.Single(w => w.Id == id);
}