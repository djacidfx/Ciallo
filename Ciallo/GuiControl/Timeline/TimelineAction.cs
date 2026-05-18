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
        NewAnimationCel.Pressed += OnNewAnimationCel;
    }

    public void Init(Entity document)
    {
        Document = document;
        var sm = document.Get<SelectionManager>();
        NewAnimationCel.VisibleIf(sm.WorkingCelFolder, e => !e.IsNull).AddTo(document);
    }

    private void OnAddCelFolder()
    {
        var folder = Document.World.Create();
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer.Value;
        // Trace from workingLayer to its ancestors
        // If we find an cel folder, parent is the folder's parent
        // If never find one, parent is the first encountered folder layer without animation
        var cursor = workingLayer.IsNull ? Document : workingLayer;
        Entity firstNonAnimFolder = Entity.Null;
        Entity animFolderParent = Entity.Null;

        while (true)
        {
            if (cursor.Has<FolderLayerSetting>())
            {
                if (cursor.Get<FolderLayerSetting>().IsCel)
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

    /// <summary>
    /// Rules: TODO
    /// </summary>
    private void OnNewAnimationCel()
    {
        var celFolder = Document.Get<SelectionManager>().WorkingCelFolder.CurrentValue;
        if (celFolder.IsNull) return;

        int frame = GetNewAnimationCelFrame(celFolder);
        var cel = Document.World.Create();

        new CommandBuilder(cel)
            .NewShapeLayer()
            .AddToLayerTree(celFolder)
            .SetWorkingLayer()
            .SetTarget(celFolder)
            .SetObservableCollection(
                e => e.Get<FolderLayerSetting>().Exposures,
                exposures => exposures.Add(frame, cel))
            .Commit();
    }


    public int GetNewAnimationCelFrame(Entity celFolder)
    {
        var exposures = celFolder.Get<FolderLayerSetting>().Exposures;
        int frame = Document.Get<SelectionManager>().CurrentFrame.Value;

        while (exposures.ContainsKey(frame))
            frame++;

        return frame;
    }
}