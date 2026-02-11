using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteShapeLayerCmd : CommandBase
{
    private Entity _parentE;
    private int _index;

    private CommandBuilder _deleteChildrenCmd;
    private readonly List<Entity> _deletedEntities = [];
    public override IEnumerable<Entity> UndoRefEntities => _deletedEntities;

    public override void BeforeFirstDo(Entity layerE)
    {
        var node = layerE.Get<LayerTreeNode>();
        _parentE = node.Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);

        _deletedEntities.Add(layerE);
        _deleteChildrenCmd = new CommandBuilder();
        foreach (var shapeE in node.Children.AsEnumerable().Reverse())
        {
            _deletedEntities.Add(shapeE);
            if (shapeE.Has<StrokeSetting>())
                _deleteChildrenCmd.SetTarget(shapeE).RemoveFromLayerTree().DeleteShape();
            else
                _deleteChildrenCmd.SetTarget(shapeE).DeleteFilledPolygon();
        }
    }

    public override void Do(Entity layerE)
    {
        // Delete children
        _deleteChildrenCmd.Do();

        // Cursor detection
        layerE.Get<ShapeBodyHolder>().RemoveFromParent();

        // View
        layerE.Get<ShapeLayerView>().RemoveFromParent();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(layerE);

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
        layerE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity layerE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, layerE);
        layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(layerE, _index);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(layerE.Get<ShapeLayerView>(), _index); // order matters

        // Body
        var worldBody = Document.Get<WorldBody>();
        worldBody.AddChild(layerE.Get<ShapeBodyHolder>());

        // Restore Children
        _deleteChildrenCmd.Undo();
    }
}