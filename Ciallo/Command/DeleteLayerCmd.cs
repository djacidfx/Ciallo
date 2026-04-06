using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;

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

    public override void BeforeFirstDo(Entity layerE)
    {
        var node = layerE.Get<LayerTreeNode>();
        if (!node.IsLeaf)
        {
            _deleteChildrenCmd = new CommandBuilder();

            foreach (var shapeE in node.Children.AsEnumerable().Reverse())
            {
                _deleteChildrenCmd.SetTarget(shapeE)
                    .RemoveFromLayerTree()
                    .DeleteShape();
                _deletedEntities.Add(shapeE);
            }
        }
        _deletedEntities.Add(layerE);
    }

    public override void Do(Entity layerE)
    {
        // If vector fill layer
        layerE.TryGet<ArrangementSynchronizationHelper>()?.Unsubscribe();

        // Delete children
        _deleteChildrenCmd?.Do();

        layerE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity layerE)
    {
        layerE.Tag<ToSerializeTag>();

        _deleteChildrenCmd?.Undo();

        layerE.TryGet<ArrangementSynchronizationHelper>()?.Subscribe();
    }
}