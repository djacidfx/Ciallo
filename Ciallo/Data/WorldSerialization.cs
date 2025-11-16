using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Ciallo.Command;
using Ciallo.Misc;
using Frent;
using Frent.Core;
using Godot;
using MessagePack;

namespace Ciallo.Data;

public static partial class AppWorldManager
{
    // Pitfall: If serialize an empty class without any [DataMember], then add a new [DataMember] later version and deserialize it back.
    // MessagePack will throw error without any useful information.
    public static readonly HashSet<Type> ToSerializeTypes = [..GetToSerializeTypes()];
    public static readonly HashSet<Type> ToSerializeTags = ToSerializeTypes.Where(t => t.IsTag()).ToHashSet();
    public static readonly HashSet<Type> ToSerializeComponents = ToSerializeTypes.Except(ToSerializeTags).ToHashSet();

    static AppWorldManager()
    {
        var registerMethod = typeof(Component).GetMethod("RegisterComponent", BindingFlags.Public | BindingFlags.Static);
        foreach (var t in ToSerializeComponents)
        {
            var genericMethod = registerMethod!.MakeGenericMethod(t);
            genericMethod.Invoke(null, null);
        }
    }

    public static void CopyWorldByData(Entity dataDocument)
    {
        // Load brushes
        var resultWorld = Create(dataDocument.Get<DocumentSetting>());
        WorkingWorld.Value = resultWorld;
        Dictionary<Entity, Entity> brushMap = [];
        foreach (var brushDataE in dataDocument.Get<BrushManager>().Brushes)
        {
            var setting = brushDataE.Get<BrushSetting>();
            var cmd = new NewBrushCmd(setting);
            cmd.Do();
            brushMap.Add(brushDataE, cmd.BrushE);
        }

        // Load layers and strokes
        var dataTreeRoot = dataDocument.Get<LayerTreeNode>();
        Dictionary<Entity, Entity> layerMap = [];
        foreach (var layerDataE in dataTreeRoot.Children)
        {
            if (layerDataE.Has<ImageLayerSetting>())
            {
                var newImageLayerCmd = new NewImageLayerCmd(layerDataE.Get<ImageLayerSetting>());
                newImageLayerCmd.Do();
                var layerE = newImageLayerCmd.InitEntity();
                layerMap.Add(layerDataE, layerE);
            }
            else if (layerDataE.Has<PolylineLayerSetting>())
            {
                var newPolylineLayerCmd = new NewPolylineLayerCmd(layerDataE.Get<PolylineLayerSetting>());
                newPolylineLayerCmd.Do();
                var layerE = newPolylineLayerCmd.LayerE;
                layerMap.Add(layerDataE, layerE);

                foreach (var polylineDataE in layerDataE.Get<LayerTreeNode>().Children)
                {
                    var geometry = polylineDataE.Get<PolylineGeometry>();

                    if (polylineDataE.Has<StrokeBrush>())
                    {
                        var newStrokeCmd = new NewStrokeCmd(layerE);
                        newStrokeCmd.Do();
                        var strokeE = newStrokeCmd.StrokeE;
                        new SetPolylineGeometryCmd(strokeE, geometry).Do();
                        var strokeBrush = polylineDataE.Get<StrokeBrush>();
                        new SetStrokeBrushCmd(strokeE, brushMap[strokeBrush.Value]).Do();
                    }
                    else if (polylineDataE.Has<FilledPolygonSetting>())
                    {
                        var setting = polylineDataE.Get<FilledPolygonSetting>();
                        var newFilledPolygonCmd = new NewFilledPolygonCmd(layerE, setting);
                        newFilledPolygonCmd.Do();
                        var polygonE = newFilledPolygonCmd.PolygonE;
                        new SetPolylineGeometryCmd(polygonE, geometry).Do();
                    }
                }
            }
        }

        // Load selection
        var dataSm = dataDocument.Get<SelectionManager>();
        new SetWorkingLayerCmd(layerMap[dataSm.WorkingLayer.CurrentValue]).Do();
        var idx = dataDocument.Get<BrushManager>().Brushes.IndexOf(dataSm.WorkingBrush.CurrentValue);
        if (idx != -1) new SetWorkingBrushCmd(idx).Do();
    }

    public static void SaveWorkingWorld()
    {
        if (WorkingDocument.CurrentValue.IsNull) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (CanSaveFile(settings.FilePath.Value))
            Save(WorkingWorld.Value, settings.FilePath.Value);
        WorkingDocument.CurrentValue.Get<CommandManager>().DocumentModified.Value = false;
    }

    public static void ReloadWorkingWorld() // for debug
    {
        if (WorkingDocument.CurrentValue.IsNull) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (!File.Exists(settings.FilePath.Value)) return;
        var world = Load(settings.FilePath.Value, out var document);
        CopyWorldByData(document);
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

        var world = Deserialize([ecBin, componentBin], out document);
        document.Get<DocumentSetting>().FilePath.Value = filePath;
        return world;
    }

    /// <remarks>
    /// Serialize all entities with <see cref="ToSerializeTag"/> and their components marked with <see cref="ToSerializeAttribute"/>.
    /// The result is two binary blobs:
    /// 1. Entity-Component structure data: List(List(Type))`, each inner list corresponds to an entity and contains its component types.
    /// 2. Component data: `Dictionary(Type, List(byte[]))`, each list contains the data of that component type for all entities in order.
    /// </remarks>
    public static byte[][] Serialize([NotNull] World world)
    {
        List<List<Type>> ecData = [];
        var query = world.CreateQuery().Tagged<ToSerializeTag>().Build();
        List<Entity> entities = [world.Document(), ..query.EnumerateWithEntities()];
        foreach (var e in entities)
        {
            List<Type> types = [];
            types.AddRange(e.TagTypes.Select(id => id.Type).Where(ToSerializeTags.Contains));
            types.AddRange(e.ComponentTypes.Select(id => id.Type).Where(ToSerializeTypes.Contains));
            ecData.Add(types);
        }
        var ecBin = MessagePackSerializer.Serialize(ecData);

        EntityToIndexFormatter.Instance.EntityList = entities;

        // Note, directly using List<object> will cause losing type information in deserialization.
        Dictionary<Type, List<byte[]>> componentData = [];

        foreach (var (idx, types) in ecData.Index())
        {
            foreach (var t in types)
            {
                if (ToSerializeTags.Contains(t)) continue;

                var obj = entities[idx].Get(t);
                if (!componentData.ContainsKey(t)) componentData[t] = [];
                var bytes = MessagePackSerializer.Serialize(t, obj);
                componentData[t].Add(bytes);
            }
        }
        var componentBin = MessagePackSerializer.Serialize(componentData);

        return [ecBin, componentBin];
    }

    public static World Deserialize(byte[][] bins, out Entity document)
    {
        var world = new World();

        var ecBin = bins[0];
        var ecData = MessagePackSerializer.Deserialize<List<List<Type>>>(ecBin);
        var entities = new List<Entity>(ecData.Count);
        foreach (var types in ecData)
        {
            var e = world.Create();
            entities.Add(e);
        }
        document = entities[0];

        EntityToIndexFormatter.Instance.EntityList = entities;

        var componentBin = bins[1];
        var componentData = MessagePackSerializer.Deserialize<Dictionary<Type, Queue<byte[]>>>(componentBin);

        foreach (var (idx, ts) in ecData.Index())
        {
            var e = entities[idx];
            foreach (var t in ts)
            {
                if (ToSerializeTags.Contains(t)) e.Tag(t);
                else
                {
                    if (!componentData.TryGetValue(t, out var dataQueue)) continue;
                    var bytes = dataQueue.Dequeue();
                    var component = MessagePackSerializer.Deserialize(t, bytes);
                    Debug.Assert(component != null, nameof(component) + " != null");
                    e.AddAs(t, component);
                }
            }
        }

        return world;
    }

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