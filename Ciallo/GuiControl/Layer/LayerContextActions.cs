using System.Collections.Generic;
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

    public static void RenameCelsByExposure(Entity celFolder)
    {
        var exposures = celFolder.Get<FolderLayerSetting>().Exposures;
        var renamedCels = new HashSet<Entity>();
        var cmd = new CommandBuilder("Rename Cels by Exposure", celFolder);
        int name = 1;

        foreach (var cel in exposures.Values)
        {
            if (!renamedCels.Add(cel))
                continue;

            cmd.SetTarget(cel)
                .SetProperty(e => e.Get<CommonLayerSetting>().Name, name.ToString());
            name++;
        }

        cmd.Commit();
    }

    public static void WrapChildrenInFolders(Entity targetFolder)
    {
        var children = targetFolder.Get<LayerTreeNode>().Children.ToArray();
        if (children.Length == 0) return;

        var document = targetFolder.Document;
        var cmd = new CommandBuilder("Wrap Children in Folders", document);
        bool targetIsCelFolder = targetFolder.Get<FolderLayerSetting>().IsCelFolder;

        foreach (var child in children)
        {
            var wrapper = document.World.Create();
            var childName = child.Get<CommonLayerSetting>().Name.Value;
            var childIndex = child.Get<LayerTreeNode>().Index;

            cmd.SetTarget(wrapper)
                .NewFolderLayer()
                .SetProperty(e => e.Get<CommonLayerSetting>().Name, childName)
                .AddToLayerTree(targetFolder, childIndex);

            if (targetIsCelFolder)
            {
                cmd.SetTarget(targetFolder)
                    .SetObservableCollection(
                        e => e.Get<FolderLayerSetting>().Exposures,
                        exposures =>
                        {
                            foreach (var (frame, cel) in exposures.ToArray())
                            {
                                if (cel == child)
                                    exposures[frame] = wrapper;
                            }
                        });
            }

            cmd.SetTarget(document)
                .MoveLayer(child, wrapper, 0);
        }

        cmd.Commit();
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

        if (targetLayer.TryGet<FolderLayerSetting>() is { IsCelFolder: false })
            return (targetLayer, -1);

        var cursor = targetLayer;
        Entity nearestCelFolder = Entity.Null;

        while (!cursor.IsDocument)
        {
            if (cursor.Has<FolderLayerSetting>())
            {
                if (cursor.Get<FolderLayerSetting>().IsCelFolder)
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
        if (!targetLayer.Tagged<CelTag>())
            return;

        var parentE = targetLayer.Get<LayerTreeNode>().ParentValue;
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
