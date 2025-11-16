using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ciallo.Command;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// World and document has one-to-one relationship.
/// In practice, a document is a special singleton entity of the world.
/// All the "document-level singleton data" should be stored in this "document" entity. (For the program-level singletons we commonly use static class).
/// The "document-level singleton data" is the data one per document, such as the DocumentSetting, LayerTree, etc.
/// </summary>
public static partial class AppWorldManager
{
    public static readonly ObservableList<World> LoadedWorlds = [];
    // Current focused document.
    public static readonly ReactiveProperty<World> WorkingWorld = new(null);
    public static readonly ReadOnlyReactiveProperty<Entity> WorkingDocument =
        WorkingWorld.Select(w => w?.Document() ?? default).ToReadOnlyReactiveProperty();
    public static bool WorkingWorldModified => WorkingWorld.Value != null && WorkingDocument.CurrentValue.Get<CommandManager>().DocumentModified.Value;

    private static readonly Dictionary<World, Entity> WorldToDocument = [];
    public static Entity Document([NotNull] this World world) => WorldToDocument[world];

    public static World Create([NotNull] DocumentSetting settings)
    {
        // Only one loaded world is supported for current version.
        Clear();
        var world = new World();

        // Init empty document
        var document = world.Create();

        // Add managers
        document.Add(settings);
        document.Add(new SelectionManager());
        document.Add(new LayerTreeNode());
        document.Add(new CommandManager());
        document.Add(new BrushManager());

        WorldToDocument.Add(world, document);

        // Always init first, then add to list
        LoadedWorlds.Add(world);

        DisplayServer.WindowSetTitle(settings.Name.Value + " - Ciallo");
        document.Get<CommandManager>().DocumentModified.Subscribe(b =>
        {
            string name = b ? "(*)" : "";
            DisplayServer.WindowSetTitle($"{name + settings.Name.Value} - Ciallo");
        }).AddTo(document);

        return world;
    }

    public static void InitialEmptyWorldForUser(World world)
    {
        AppBrushLibrary.SelectedIndex.Value = 0;

        new NewPolylineLayerCmd { WorkingWorld = world }
            .Combine(new SetWorkingLayerCmd(0))
            .DoAllCombination();
    }

    public static void Remove([NotNull] World world)
    {
        if (!LoadedWorlds.Contains(world)) throw new KeyNotFoundException("The specified world does not exist.");

        DisplayServer.WindowSetTitle("Ciallo");

        // Remove working world
        LoadedWorlds.Remove(world);
        if (WorkingWorld.Value == world) WorkingWorld.Value = LoadedWorlds.Count > 0 ? LoadedWorlds[0] : null;

        // Dispose or free managers
        world.Document().Get<CommandManager>().Free();

        // Dispose world
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

    // If false, user cancels the close operation.
    public static async Task<bool> UserCloseWorkingWorld()
    {
        if (WorkingWorld.Value == null) return true;

        if (WorkingWorldModified)
        {
            var dialog = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<SaveChangeDialog>().Single();
            var result = await dialog.PopupCollectInput();
            if (result == 1) // Yes
            {
                SaveWorkingWorld();
                Remove(WorkingWorld.Value);
                return true;
            }
            if (result == 0) // No
            {
                Remove(WorkingWorld.Value);
                return true;
            }
            // Cancel
            return false;
        }
        Remove(WorkingWorld.Value);
        return true;
    }
}