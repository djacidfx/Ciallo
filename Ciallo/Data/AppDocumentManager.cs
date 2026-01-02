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
    public static readonly ObservableList<Entity> LoadedDocuments = [];

    public static readonly ReactiveProperty<Entity> WorkingDocument = new(Entity.Null);

    public static bool WorkingDocumentModified => !WorkingDocument.Value.IsNull && WorkingDocument.Value.Get<CommandManager>().DocumentModified.Value;

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
        document.Add(new LayerTreeNode()); // Document entity is layer tree root
        // Add managers
        document.Add(new SelectionManager());
        document.Add(new CommandManager());
        document.Add(new BrushManager());
        document.Add(new ToolManager());

        WorldToDocument.Add(world, document);

        // Always init first, then add to list
        LoadedDocuments.Add(document);

        document.Get<CommandManager>().DocumentModified
            .CombineLatest(settings.Name, (modified, name) => (modified, name)).Subscribe(v =>
            {
                string prepend = v.modified ? "(*)" : "";
                DisplayServer.WindowSetTitle($"{prepend + v.name} - Ciallo");
            }).AddTo(document);

        return document;
    }

    public static void InitialEmptyWorldForUser(Entity document)
    {
        AppBrushLibrary.SelectedIndex.Value = 0;

        new CommandBuilder(document.World.Create())
            .NewPolylineLayer()
            .SetWorkingLayer()
            .Do();
        if (AppBrushLibrary.BrushSettings.Count > 0)
            AppBrushLibrary.SelectedIndex.Value = 0;
        document.Get<ToolManager>().ActivatePaintTool();
    }

    public static void Remove(Entity document)
    {
        if (!LoadedDocuments.Contains(document)) throw new KeyNotFoundException("The specified world does not exist.");

        DisplayServer.WindowSetTitle("Ciallo");

        // Remove working world
        LoadedDocuments.Remove(document);
        if (WorkingDocument.Value == document)
            WorkingDocument.Value = LoadedDocuments.Count > 0 ? LoadedDocuments[0] : Entity.Null;

        // Dispose or free managers
        document.Get<CommandManager>().Free();

        // Dispose world
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
    public static async Task<bool> UserCloseWorkingWorld()
    {
        if (WorkingDocument.Value.IsNull) return true;

        if (WorkingDocumentModified)
        {
            var dialog = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<SaveChangeDialog>()
                .Single();
            var result = await dialog.PopupCollectInput();
            if (result == 1) // Yes
            {
                SaveWorkingWorld();
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