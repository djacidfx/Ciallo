using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Misc;
using Godot.Collections;
using MessagePack;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// We consider world and document has one-to-one relationship.
/// In practice, a document is a special singleton entity of the world.
/// All the "document-level singleton data" should be stored in this "document" entity. (For the program-level singletons we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as the DocumentSetting, LayerTree, etc.
/// </summary>
public static class AppWorldManager
{
    public static readonly ObservableList<World> LoadedWorlds = [];
    // Current focused document.
    public static readonly ReactiveProperty<World> WorkingWorld = new(null);
    public static readonly ReadOnlyReactiveProperty<Entity> WorkingDocument =
        WorkingWorld.Select(w => w?.Document() ?? Entity.Null).ToReadOnlyReactiveProperty();
    
    private static readonly System.Collections.Generic.Dictionary<World, Entity> DocumentSingletons = [];
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
        var brushManager = new BrushManager();
        document.Add(settings, layerTreeManager, selectionManager, commandManager, brushManager);
        
        // Set as working world
        WorkingWorld.Value = world;
        // Always init first, then add to list
        LoadedWorlds.Add(world);
        
        // Add initial layer
        AppBrushLibrary.SelectedIndex.Value = 0;
        new NewPolylineLayerCmd([0]).Do();
        new ChangeWorkingLayerCmd([0]).Do();
        
        return world;
    }

    public static void Remove([NotNull] World world)
    {
        if (!LoadedWorlds.Contains(world)) throw new KeyNotFoundException("The specified world does not exist.");
        
        // Remove working world
        LoadedWorlds.Remove(world);
        if(WorkingWorld.Value == world) WorkingWorld.Value = LoadedWorlds.Count > 0 ? LoadedWorlds[0] : null;

        // Dispose or free managers
        world.Document().Get<CommandManager>().Free();
        
        // Dispose world
        DocumentSingletons.Remove(world);
        world.Dispose();
    }
    
    public static void Clear()
    {
        // Don't use `clear` on LoadedWorlds since it will trigger reset rather than remove event.
        foreach (var world in LoadedWorlds.ToList())
        {
            Remove(world);
        }
    }
    
    public static void SaveCurrentDocument()
    {
        if (WorkingDocument.CurrentValue == Entity.Null) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        bool canSave = CanSaveFile(settings.FilePath);
        if(CanSaveFile(settings.FilePath)) 
            Save(WorkingWorld.Value, settings.FilePath);
    }

    public static void Save(World world, string filePath)
    {
        var bins = Serialize(world);
        var writer = new ZipPacker();
        var err = writer.Open(filePath);
        if (err != Error.Ok) throw new InvalidOperationException($"Cannot open file {filePath} for writing.");
        writer.StartFile("EntityComponent.bin");
        writer.WriteFile(bins[0]);
        writer.CloseFile();
        writer.StartFile("ComponentData.bin");
        writer.WriteFile(bins[1]);
        writer.CloseFile();
        writer.Close();
    }
    
    public static Array<Byte[]> Serialize([NotNull] World world)
    {
        List<Entity> entities = [];
        world.Query(in new QueryDescription().WithAll<ToSerializeTag>(), e => entities.Add(e));
        List<List<Type>> ecData = [];
        foreach (var e in entities)
        {
            var componentTypes = e.GetComponentTypes().Components.ToArray().Select(ct => ct.Type)
                .Where(t => ToSerializeTypes.Contains(t));
    
            ecData.Add(componentTypes.ToList());
        }
        var ecBin = MessagePackSerializer.Serialize(ecData);
        EntityToIndexFormatter.Instance.EntityList = entities;
        
        System.Collections.Generic.Dictionary<Type, List<object>> componentData = [];
        var tagTypes = ToSerializeTypes.Where(t => t.IsTag()).ToHashSet();
        foreach (var e in entities)
        {
            var types = e.GetComponentTypes().Components.ToArray().Select(ct => ct.Type).ToArray();
            var data = e.GetAllComponents();
            for(int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (!ToSerializeTypes.Contains(t) || tagTypes.Contains(t)) continue; 
                if(!componentData.ContainsKey(t))
                    componentData[t] = [];
                componentData[t].Add(data[i]);
            }
        }
        var componentBin = MessagePackSerializer.Serialize(componentData);
        
        return [ecBin, componentBin];
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

    public static readonly HashSet<Type> ToSerializeTypes = [..GetToSerializeTypes()];
    public static IEnumerable<Type> GetToSerializeTypes()
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
        
        return allTypes.Where(t => t!.GetCustomAttributes(typeof(ToSerializeAttribute), false).Length > 0);
    }
    
    public static bool IsTag(this Type type)
    {
        if (!type.IsValueType || type.IsEnum || type.IsPrimitive)
            return false;
    
        var fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly
        );

        return fields.Length == 0;
    }
    
    public static bool CanSaveFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory)) return false;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        try
        {
            // has write permission.
            using var x = File.Create(filePath, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch
        {
            return false;
        }
    }
}