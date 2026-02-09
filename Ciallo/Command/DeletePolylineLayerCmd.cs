using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeletePolylineLayerCmd : CommandBase
{
    private Entity _parentE;
    private int _index;

    private CommandBuilder _deleteChildrenCmd;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity layerE)
    {
        var node = layerE.Get<LayerTreeNode>();
        _parentE = node.Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);

        _deleteChildrenCmd = new CommandBuilder();
        foreach (var polylineE in node.Children.AsEnumerable().Reverse())
        {
            if (polylineE.Has<StrokeSetting>())
                _deleteChildrenCmd.SetTarget(polylineE).LayerRemoveStroke().DeleteStroke();
            else
                _deleteChildrenCmd.SetTarget(polylineE).DeleteFilledPolygon();
        }
    }

    public override void Do(Entity layerE)
    {
        // Delete children
        _deleteChildrenCmd.Do();

        // Cursor detection
        layerE.Get<PolylineBodyHolder>().RemoveFromParent();

        // View
        layerE.Get<PolylineLayerView>().RemoveFromParent();

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
        worldView.InsertNodeAt(layerE.Get<PolylineLayerView>(), _index); // order matters

        // Body
        var worldArea = Document.Get<WorldBody>();
        worldArea.AddChild(layerE.Get<PolylineBodyHolder>());

        // Restore Children
        _deleteChildrenCmd.Undo();
    }
}