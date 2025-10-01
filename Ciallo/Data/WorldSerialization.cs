using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Misc;
using Godot;
using Godot.Collections;
using MessagePack;

namespace Ciallo.Data;
using Sys = System.Collections.Generic;

public static partial class AppWorldManager
{
    public static readonly Sys.HashSet<Type> ToSerializeTypes = [..GetToSerializeTypes()];
    public static readonly Sys.HashSet<Type> ToSerializeTags = ToSerializeTypes.Where(t => t.IsTag()).ToHashSet();

    public static void SaveCurrentDocument()
    {
        if (WorkingDocument.CurrentValue == Entity.Null) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (CanSaveFile(settings.FilePath))
            Save(WorkingWorld.Value, settings.FilePath);
    }

    public static void LoadCurrentDocument() // for debug
    {
        if(WorkingDocument.CurrentValue == Entity.Null) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (!File.Exists(settings.FilePath)) return;
        var world = Load(settings.FilePath, out var document);
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
    
    public static World Load(string filePath, out Entity document)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File {filePath} not found.");
        var reader = new ZipReader();
        var err = reader.Open(filePath);
        var ecBin = reader.ReadFile("EntityComponent.bin");
        var componentBin = reader.ReadFile("ComponentData.bin");
        reader.Close();

        return Deserialize([ecBin, componentBin], out document);
    }

    /// <remarks>
    /// Serialize all entities with <see cref="ToSerializeTag"/> and their components marked with <see cref="ToSerializeAttribute"/>.
    /// The result is two binary blobs:
    /// 1. Entity-Component structure data: ListListType`, each inner list corresponds to an entity and contains its component types.
    /// 2. Component data: `Dictionary(Type, List(object))`, each list contains the data of that component type for all entities in order.
    /// </remarks>
    public static Array<byte[]> Serialize([NotNull] World world)
    {
        Sys.List<Entity> entities = [world.Document()];
        world.Query(in new QueryDescription().WithAll<ToSerializeTag>(), e => entities.Add(e));
        Sys.List<Sys.List<Type>> ecData = [];
        foreach (var e in entities)
        {
            var componentTypes = e.GetComponentTypes().Components.ToArray().Select(ct => ct.Type)
                .Where(t => ToSerializeTypes.Contains(t));

            ecData.Add(componentTypes.ToList());
        }

        var ecBin = MessagePackSerializer.Serialize(ecData);
        EntityToIndexFormatter.Instance.EntityList = entities;

        // Note, directly using List<object> will cause losing type information in deserialization.
        Sys.Dictionary<Type, Sys.List<byte[]>> componentData = [];
        foreach (var e in entities)
        {
            var types = e.GetComponentTypes().Components.ToArray().Select(ct => ct.Type).ToArray();
            var data = e.GetAllComponents();
            
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (!ToSerializeTypes.Contains(t) || ToSerializeTags.Contains(t)) continue;
                if (!componentData.ContainsKey(t))
                    componentData[t] = [];
                
                componentData[t].Add(MessagePackSerializer.Serialize(data[i]));
            }
        }
        
        var componentBin = MessagePackSerializer.Serialize(componentData);

        return [ecBin, componentBin];
    }

    public static World Deserialize(Array<byte[]> bins, out Entity document)
    {
        var world = World.Create();
        world.AddForbiddenComponents();

        var ecBin = bins[0];
        var ecData = MessagePackSerializer.Deserialize<Sys.List<Sys.List<Type>>>(ecBin);
        var entities = new Sys.List<Entity>(ecData.Count);
        foreach (var types in ecData)
        {
            ComponentType[] cTypes = types.Select(t => ComponentRegistry.TypeToComponentType[t]).ToArray();
            // var e = world.Create(new Signature(cTypes));// Don't work, created entity loses type information.
            var e = world.Create();
            e.AddRange(cTypes);
            entities.Add(e);
        }
        document = entities[0];

        EntityToIndexFormatter.Instance.EntityList = entities;

        var componentBin = bins[1];
        var componentData = MessagePackSerializer.Deserialize<Sys.Dictionary<Type, Sys.Queue<byte[]>>>(componentBin);
        foreach (var e in entities)
        {
            foreach (var ct in e.GetComponentTypes().Components)
            {
                var type = ct.Type;
                if (!componentData.TryGetValue(type, out var dataQueue)) continue;
                var data = dataQueue.Dequeue();
                if (data == null) continue;
                var component = MessagePackSerializer.Deserialize(type, data);
                if(component != null) e.Set(component);
            }
        }

        return world;
    }

    public static Sys.IEnumerable<Type> GetToSerializeTypes()
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