using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Ciallo.Command;
using Frent;
using Frent.Core;
using Godot;
using MessagePack;

namespace Ciallo.Data;

public static partial class AppDocumentManager
{
    // Pitfall: If serialize an empty class without any [DataMember], then add a new [DataMember] later version and deserialize it back.
    // MessagePack will throw error without any useful information.
    public static readonly HashSet<Type> ToSerializeTypes = [.. GetToSerializeTypes()];
    public static readonly HashSet<Type> ToSerializeTags = ToSerializeTypes.Where(t => t.IsTag()).ToHashSet();
    public static readonly HashSet<Type> ToSerializeComponents = ToSerializeTypes.Except(ToSerializeTags).ToHashSet();

    static AppDocumentManager()
    {
        var registerMethod =
            typeof(Component).GetMethod("RegisterComponent", BindingFlags.Public | BindingFlags.Static);
        foreach (var t in ToSerializeComponents)
        {
            var genericMethod = registerMethod!.MakeGenericMethod(t);
            genericMethod.Invoke(null, null);
        }
    }

    // Slop design here
    public static void CopyWorldByData(Entity dataDocument)
    {
        Dictionary<Entity, Entity> entityMap = new() { { Entity.Null, Entity.Null } };

        var resultDocument = Create(dataDocument.Get<DocumentSetting>());
        entityMap.Add(dataDocument, resultDocument);
        WorkingDocument.Value = resultDocument;
        // Pre-create all result entities upfront (mirrors Serialize's Tagged<ToSerializeTag> query)
        var dataWorld = dataDocument.World;
        var resultWorld = resultDocument.World;
        foreach (var dataE in dataWorld.CreateQuery().Build().EnumerateWithEntities())
            entityMap.TryAdd(dataE, resultWorld.Create());

        // Load brushes
        var loadBrushCmd = new CommandBuilder();
        foreach (var strokeBrushDataE in dataDocument.Get<BrushManager>().StrokeBrushEs)
            loadBrushCmd.SetTarget(entityMap[strokeBrushDataE]).NewStrokeBrush(strokeBrushDataE);
        foreach (var vectorFillBrushDataE in dataDocument.Get<BrushManager>().VectorFillBrushEs)
            loadBrushCmd.SetTarget(entityMap[vectorFillBrushDataE]).NewVectorFillBrush(vectorFillBrushDataE);
        loadBrushCmd.Do();

        // Load layers and strokes
        LoadChildren(dataDocument, resultDocument);

        void LoadChildren(Entity dataParentE, Entity resultParentE)
        {
            foreach (var layerDataE in dataParentE.Get<LayerTreeNode>().Children)
                LoadLayer(layerDataE, resultParentE);
        }

        void LoadLayer(Entity layerDataE, Entity resultParentE)
        {
            var layerResultE = entityMap[layerDataE];
            if (layerDataE.Has<FolderLayerSetting>())
            {
                if (layerDataE.Get<FolderLayerSetting>().IsCel)
                {
                    var dataExposures = layerDataE.Get<FolderLayerSetting>().Exposures;
                    foreach (var (frame, exposedE) in dataExposures.ToArray())
                    {
                        dataExposures[frame] = entityMap[exposedE];
                    }
                }

                new CommandBuilder(layerResultE)
                    .NewFolderLayer(layerDataE)
                    .AddToLayerTree(resultParentE)
                    .Do();
                LoadChildren(layerDataE, layerResultE);
            }
            else if (layerDataE.Has<ImageLayerSetting>())
            {
                new CommandBuilder(layerResultE)
                    .NewImageLayer(layerDataE)
                    .AddToLayerTree(resultParentE)
                    .Do();
            }
            else if (layerDataE.Has<ShapeLayerSetting>())
            {
                new CommandBuilder(layerResultE)
                    .NewShapeLayer(layerDataE)
                    .AddToLayerTree(resultParentE)
                    .Do();

                foreach (var shapeDataE in layerDataE.Get<LayerTreeNode>().Children)
                {
                    if (shapeDataE.Has<StrokeSetting>())
                    {
                        var brushRef = shapeDataE.Get<StrokeSetting>().BrushE;
                        brushRef.Value = entityMap[brushRef.Value];
                        new CommandBuilder(entityMap[shapeDataE])
                            .NewStroke(shapeDataE)
                            .AddToLayerTree(layerResultE)
                            .Do();
                    }
                    else if (shapeDataE.Has<FilledPolygonSetting>())
                    {
                        var brushRef = shapeDataE.Get<FilledPolygonSetting>().BrushE;
                        brushRef.Value = entityMap[brushRef.Value];
                        new CommandBuilder(entityMap[shapeDataE])
                            .NewFilledPolygon(shapeDataE)
                            .AddToLayerTree(layerResultE)
                            .Do();
                    }
                }
            }
            else if (layerDataE.Has<VectorFillLayerSetting>())
            {
                new CommandBuilder(layerResultE)
                    .NewVectorFillLayer(layerDataE)
                    .AddToLayerTree(resultParentE)
                    .Do();

                foreach (var markerDataE in layerDataE.Get<LayerTreeNode>().Children)
                {
                    markerDataE.Get<VectorFillMarkerSetting>().BrushE.Value =
                        entityMap[markerDataE.Get<VectorFillMarkerSetting>().BrushE.Value];
                    new CommandBuilder(entityMap[markerDataE])
                        .NewVectorFillMarker(markerDataE)
                        .AddToLayerTree(layerResultE)
                        .Do();
                }
            }
        }

        // Vector fil reference layers remap
        foreach (var dataE in dataDocument.World.Query<VectorFillLayerSetting>().EnumerateWithEntities())
        {
            var resultE = entityMap[dataE];
            var newEs = dataE.Get<VectorFillLayerSetting>().ReferenceLayers.Select(e => entityMap[e]);
            resultE.Get<VectorFillLayerSetting>().ReferenceLayers.AddRange(newEs);
        }

        // Load selection
        var loadSelectionCmd = new CommandBuilder();
        var dataSm = dataDocument.Get<SelectionManager>();
        loadSelectionCmd.SetTarget(entityMap[dataSm.WorkingLayer.CurrentValue])
            .SetWorkingLayer();

        var dataStrokeBrushE = dataSm.WorkingStrokeBrush.Value;
        if (!dataStrokeBrushE.IsNull)
            loadSelectionCmd.SetTarget(entityMap[dataStrokeBrushE]).SetWorkingStrokeBrush().Do();
        var dataVectorFillBrushE = dataSm.WorkingVectorFillBrush.Value;
        if (!dataVectorFillBrushE.IsNull)
            loadSelectionCmd.SetTarget(resultDocument)
                .SetProperty(e => e.Get<SelectionManager>().WorkingVectorFillBrush, entityMap[dataVectorFillBrushE]);
        loadSelectionCmd.SetTarget(resultDocument)
            .SetProperty(e => e.Get<SelectionManager>().CurrentFrame, dataSm.CurrentFrame.Value);

        loadSelectionCmd.Do();

        // Load timeline setting
        resultDocument.Get<TimelineSetting>().CopyFrom(dataDocument.Get<TimelineSetting>());
    }

    public static void SaveWorkingDocument()
    {
        if (WorkingDocument.CurrentValue.IsNull) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (CanSaveFile(settings.FilePath.Value))
            Save(WorkingDocument.Value, settings.FilePath.Value);
        WorkingDocument.CurrentValue.Get<CommandManager>().OnSave();
    }

    public static void SaveWorkingDocumentAs(string filePath)
    {
        if (WorkingDocument.CurrentValue.IsNull) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (CanSaveFile(filePath))
        {
            Save(WorkingDocument.Value, filePath);
            settings.FilePath.Value = filePath;
            settings.Name.Value = filePath.GetFile().GetBaseName();
            WorkingDocument.CurrentValue.Get<CommandManager>().OnSave();
        }
    }

    public static void ReloadWorkingDocument() // for debug
    {
        if (WorkingDocument.CurrentValue.IsNull) return;
        var settings = WorkingDocument.CurrentValue.Get<DocumentSetting>();
        if (!File.Exists(settings.FilePath.Value)) return;
        var document = Load(settings.FilePath.Value);
        CopyWorldByData(document);
    }

    public static void Save(Entity document, string filePath)
    {
        var bins = Serialize(document.World);
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

    public static Entity Load(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File {filePath} not found.");
        var reader = new ZipReader();
        var err = reader.Open(filePath);
        var ecBin = reader.ReadFile("EntityComponent.bin");
        var componentBin = reader.ReadFile("ComponentData.bin");
        reader.Close();

        var document = Deserialize([ecBin, componentBin]);
        document.Get<DocumentSetting>().FilePath.Value = filePath;
        return document;
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
        List<Entity> entities = [world.Document(), .. query.EnumerateWithEntities()];
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

    public static Entity Deserialize(byte[][] bins)
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

        var document = entities[0];

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

        return document;
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