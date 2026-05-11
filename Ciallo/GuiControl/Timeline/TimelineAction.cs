using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class TimelineAction : Container
{
    public Entity Document;

    public override void _Ready()
    {
        AddCelFolder.Pressed += OnAddCelFolder;
    }

    private void OnAddCelFolder()
    {
        var folder = Document.World.Create();
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer.Value;
        // Trace from workingLayer to it's ancestors
        // If we find an animation folder, parent is the folder's parent
        // If never find one, parent is the first encountered folder layer without animation
        var cursor = workingLayer.IsNull ? Document : workingLayer;
        Entity firstNonAnimFolder = Entity.Null;
        Entity animFolderParent = Entity.Null;

        while (true)
        {
            if (cursor.Has<FolderLayerSetting>())
            {
                if (cursor.TryGet<FolderLayerSetting>().IsCelFolder.Value)
                {
                    animFolderParent = cursor.Get<LayerTreeNode>().ParentValue;
                    break;
                }
                if (firstNonAnimFolder.IsNull)
                    firstNonAnimFolder = cursor;
            }
            if (cursor.IsDocument) break;
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        var parent = animFolderParent.IsNull ? firstNonAnimFolder : animFolderParent;

        new CommandBuilder(folder)
            .NewCelFolder()
            .AddToLayerTree(parent)
            .Commit();
    }
}