using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Frent.Systems;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteLayerCmd : CommandBase
{
    private CommandBuilder _deleteChildrenCmd;
    private readonly List<Entity> _deletedEntities = [];

    public override void OnDeletedAsUndo()
    {
        foreach (var e in _deletedEntities)
            e.Delete();
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        var node = targetE.Get<LayerTreeNode>();
        if (!node.IsLeaf)
        {
            _deleteChildrenCmd = new CommandBuilder();

            foreach (var childE in node.Children.AsEnumerable().Reverse())
            {
                if (childE.Has<FolderLayerSetting>())
                    _deleteChildrenCmd.SetTarget(childE)
                        .RemoveFromLayerTree()
                        .DeleteLayer();
                else
                    _deleteChildrenCmd.SetTarget(childE)
                        .RemoveFromLayerTree()
                        .DeleteShape();

                CollectAllDescendants(childE, _deletedEntities);
            }
        }
        _deletedEntities.Add(targetE);
    }

    // Post-order: children before the node itself, matching OnDeletedAsUndo deletion order.
    private static void CollectAllDescendants(Entity e, List<Entity> list)
    {
        foreach (var childE in e.Get<LayerTreeNode>().Children)
            CollectAllDescendants(childE, list);
        list.Add(e);
    }

    public override void Do(Entity targetE)
    {
        // If vector fill layer
        targetE.TryGet<ArrangementSynchronizationHelper>()?.Unsubscribe();
        
        // Delete children
        _deleteChildrenCmd?.Do();

        // Remove shape layer from vector fill layer settings
        if (targetE.Has<ShapeLayerSetting>())
        {
            var query = targetE.World.Query<VectorFillLayerSetting>();
            query.Delegate((ref VectorFillLayerSetting setting) =>
            {
                setting.ReferenceLayers.Remove(targetE);
            });
        }

        targetE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity layerE)
    {
        layerE.Tag<ToSerializeTag>();

        _deleteChildrenCmd?.Undo();

        layerE.TryGet<ArrangementSynchronizationHelper>()?.Subscribe();
    }
}