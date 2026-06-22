using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ciallo.Command;
using Ciallo.GuiControl;
using Ciallo.Tool;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// A document entity is a special singleton entity of a world object.
/// All the "document-level singleton data" should be stored in this "document" entity. (For the program-level singletons we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as DocumentSetting, CommandManager, etc.
/// The document entity also acts as the root of the layer tree.
/// </summary>
public static partial class AppDocumentManager
{
    public static readonly ObservableHashSet<Entity> LoadedDocuments = [];

    public static readonly ReactiveProperty<Entity> WorkingDocument = new(Entity.Null);

    public static bool WorkingDocumentModified => !WorkingDocument.Value.IsNull &&
                                                  WorkingDocument.Value.Get<CommandManager>().DocumentModified.CurrentValue;
    private static readonly Dictionary<World, Entity> WorldToDocument = [];
    public static Entity Document([NotNull] this World world) => WorldToDocument[world];

    public static Entity Create([NotNull] DocumentSetting settings)
    {
        // Only one loaded world is supported for current version.
        Clear();
        var world = new World();

        // Init empty document
        var document = world.Create();

        document.Add(settings);
        var root = new LayerTreeNode();
        document.Add(root); // Document entity is layer tree root
        document.Add(new FolderLayerSetting()); // Document entity is also a folder layer
        document.Add(new TimelineSetting());
        // Add managers
        document.Add(new SelectionManager());
        document.Get<SelectionManager>().InitWorkingCelFolder(root);
        document.Add(new CommandManager());
        document.Add(new BrushManager());
        document.Add(new ToolManager());
        document.Get<ToolManager>().ObserveTimelineRolling(document.Get<TimelineSetting>().IsRollingFrame);

        WorldToDocument.Add(world, document);

        // Always init first, then add to list
        LoadedDocuments.Add(document);

        document.Get<CommandManager>().DocumentModified
            .CombineLatest(settings.Name, (modified, name) => (modified, name))
            .Subscribe(v =>
            {
                string prepend = v.modified ? "(*)" : "";
                DisplayServer.WindowSetTitle($"{prepend + v.name} - Ciallo");
            }).AddTo(document);

        return document;
    }

    public static void InitialEmptyAnimationDocumentForUser(Entity document)
    {
        // Cel folder layer has one cel holding a shape layer
        var celFolder = document.World.Create();
        var cel = document.World.Create();
        var shapeLayer = document.World.Create();
        var cmd = new CommandBuilder("Create Initial Animation Document", celFolder)
            .NewCelFolder()
            .AddToLayerTree(document)
            .SetTarget(cel)
            .NewFolderLayer()
            .SetProperty(e => e.Get<CommonLayerSetting>().Name, "1")
            .AddToLayerTree(celFolder)
            .SetTarget(shapeLayer)
            .NewShapeLayer()
            .AddToLayerTree(cel)
            .SetTarget(celFolder)
            .SetObservableCollection(e => e.Get<FolderLayerSetting>().Exposures, exposures => exposures.Add(0, cel))
            .SetTarget(shapeLayer)
            .SetWorkingLayer(true);

        // Fill brush
        Color[] colors = [Colors.PaleTurquoise, Colors.LightGreen, Colors.LemonChiffon, Colors.LightPink];
        for (int i = 0; i < colors.Length; i++)
        {
            string path = $"res://Rendering/Image/Bullseye{i}.svg";
            var img = GD.Load<Image>(path);
            var tex = ImageTexture.CreateFromImage(img);
            var brush = document.World.Create();
            cmd.SetTarget(brush)
                .NewVectorFillBrush()
                .SetProperty(e => e.Get<FillBrushSetting>().MarkerTexture, tex)
                .SetProperty(e => e.Get<FillBrushSetting>().FillColor, colors[i]);
            if (i == 0)
                cmd.SetProperty(e => e.Document.Get<SelectionManager>().WorkingVectorFillBrush, brush);
        }

        // Stroke brush
        // Guard when users delete all built-in brushes for whatever reason.
        if (AppStrokeBrushLibrary.BrushSettings.Count > 0)
        {
            var strokeBrush = document.World.Create();
            cmd.SetTarget(strokeBrush)
            .NewStrokeBrush(AppStrokeBrushLibrary.BrushSettings[0])
            .SetWorkingStrokeBrush();
        }

        cmd.Do();
        document.Get<ToolManager>().ActivatePaintTool();
    }

    public static void Remove(Entity document)
    {
        if (!LoadedDocuments.Contains(document)) throw new KeyNotFoundException("The specified world does not exist.");

        DisplayServer.WindowSetTitle("Ciallo");

        document.Get<ToolManager>().DeactivateWorkingTool();
        // Signal working world removal
        LoadedDocuments.Remove(document);
        if (WorkingDocument.Value == document)
            WorkingDocument.Value = Entity.Null;

        // Dispose world
        // Warning: Dispose a world don't trigger it's entities' deletion events.
        var allQuery = document.World.CreateQuery().Build();
        foreach (Entity e in allQuery.EnumerateWithEntities())
            e.Delete();
        document.World.Dispose();
    }

    public static void Clear()
    {
        // Don't use `clear` on LoadedWorlds since it will trigger reset rather than remove event.
        foreach (var document in LoadedDocuments.ToList())
        {
            Remove(document);
        }
    }

    // If false, user cancels the close operation.
    public static async Task<bool> UserCloseWorkingDocument()
    {
        if (WorkingDocument.Value.IsNull) return true;

        if (WorkingDocumentModified)
        {
            var result = await AppDialogHost.SaveChangeDialog.PopupCollectInput();
            if (result == 1) // Yes
            {
                if (!SaveWorkingDocument())
                    return false;
                Remove(WorkingDocument.Value);
                return true;
            }

            if (result == 0) // No
            {
                Remove(WorkingDocument.Value);
                return true;
            }

            // Cancel
            return false;
        }

        Remove(WorkingDocument.Value);
        return true;
    }
}
