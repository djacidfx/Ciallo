using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;

namespace Ciallo.GuiControl;

internal static class LayerContextActions
{
    public static void NewShapeLayer(Entity targetLayer)
    {
        var document = targetLayer.Document;
        var (parentE, index) = GetNewLayerInsertPosition(targetLayer);
        new CommandBuilder(document.World.Create())
            .NewShapeLayer()
            .AddToLayerTree(parentE, index)
            .SetWorkingLayer()
            .Commit();
    }

    public static void NewFolderLayer(Entity targetLayer)
    {
        var document = targetLayer.Document;
        var (parentE, index) = GetNewLayerInsertPosition(targetLayer);
        new CommandBuilder(document.World.Create())
            .NewFolderLayer()
            .AddToLayerTree(parentE, index)
            .SetWorkingLayer()
            .Commit();
    }

    public static void NewCelFolderLayer(Entity targetLayer)
    {
        var document = targetLayer.Document;
        var (parentE, index) = GetNewCelFolderInsertPosition(targetLayer);
        new CommandBuilder(document.World.Create())
            .NewCelFolder()
            .AddToLayerTree(parentE, index)
            .SetWorkingLayer()
            .Commit();
    }

    public static void DeleteLayer(Entity targetLayer)
    {
        var nextLayer = GetNextFocusLayerAfterDeletion(targetLayer);
        var cmd = new CommandBuilder(nextLayer)
            .SetWorkingLayer();

        RemoveParentCelFolderExposures(cmd, targetLayer);

        cmd.SetTarget(targetLayer)
            .RemoveFromLayerTree()
            .DeleteLayer()
            .Commit();
    }

    public static void UngroupFolder(Entity targetFolder)
    {
        if (!targetFolder.Has<FolderLayerSetting>()) return;

        var folderNode = targetFolder.Get<LayerTreeNode>();
        var parentE = folderNode.ParentValue;
        if (parentE.IsNull) return;

        var children = folderNode.Children.ToArray();
        var nextLayer = GetNextFocusLayerAfterDeletion(targetFolder);
        int insertIndex = folderNode.Index;

        var cmd = new CommandBuilder(nextLayer)
            .SetWorkingLayer();

        RemoveParentCelFolderExposures(cmd, targetFolder);

        for (int i = 0; i < children.Length; i++)
        {
            cmd.SetTarget(targetFolder.Document)
                .MoveLayer(children[i], parentE, insertIndex + i);
        }

        cmd.SetTarget(targetFolder)
            .RemoveFromLayerTree()
            .DeleteLayer()
            .Commit();
    }

    private static (Entity parentE, int index) GetNewLayerInsertPosition(Entity targetLayer)
    {
        if (targetLayer.IsNull || targetLayer.IsDocument)
            return (AppDocumentManager.WorkingDocument.Value, -1);

        if (targetLayer.Has<FolderLayerSetting>())
            return (targetLayer, -1);

        var layerNode = targetLayer.Get<LayerTreeNode>();
        return (layerNode.ParentValue, layerNode.Index + 1);
    }

    private static (Entity parentE, int index) GetNewCelFolderInsertPosition(Entity targetLayer)
    {
        if (targetLayer.IsNull || targetLayer.IsDocument)
            return (AppDocumentManager.WorkingDocument.Value, -1);

        if (targetLayer.TryGet<FolderLayerSetting>() is { IsCel: false })
            return (targetLayer, -1);

        var cursor = targetLayer;
        Entity nearestCelFolder = Entity.Null;

        while (!cursor.IsDocument)
        {
            if (cursor.Has<FolderLayerSetting>())
            {
                if (cursor.Get<FolderLayerSetting>().IsCel)
                {
                    nearestCelFolder = cursor;
                    break;
                }
            }

            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        if (!nearestCelFolder.IsNull)
        {
            var celFolderNode = nearestCelFolder.Get<LayerTreeNode>();
            return (celFolderNode.ParentValue, celFolderNode.Index + 1);
        }

        var layerNode = targetLayer.Get<LayerTreeNode>();
        return (layerNode.ParentValue, layerNode.Index + 1);
    }

    private static Entity GetNextFocusLayerAfterDeletion(Entity targetLayer)
    {
        var document = targetLayer.Document;
        var root = document.Get<LayerTreeNode>();
        var targetPath = root.FindPathTo(targetLayer);
        var nextLayerPath = root.GetNextFocusPathAfterDeletion(targetPath);
        return nextLayerPath.IsEmpty ? document : root.GetDescendant(nextLayerPath);
    }

    private static void RemoveParentCelFolderExposures(CommandBuilder cmd, Entity targetLayer)
    {
        var parentE = targetLayer.Get<LayerTreeNode>().ParentValue;
        if (parentE.TryGet<FolderLayerSetting>()?.IsCel != true)
            return;

        cmd.SetTarget(parentE)
            .SetObservableCollection(
                e => e.Get<FolderLayerSetting>().Exposures,
                exposures =>
                {
                    foreach (var (frame, celE) in exposures.ToArray())
                    {
                        if (celE == targetLayer)
                            exposures.Remove(frame);
                    }
                });
    }
}
