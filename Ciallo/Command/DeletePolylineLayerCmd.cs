using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeletePolylineLayerCmd : CommandBase
{
    private Entity _parentE;
    private int _index;

    private CommandBuilder _deleteChildrenCmd;
    private PolylineLayerView _layerView;
    private PolylineBodyHolder _bodyHolder;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => [_layerView, _bodyHolder];

    public override void BeforeFirstDo(Entity layerE)
    {
        var node = layerE.Get<LayerTreeNode>();
        _parentE = node.Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);

        _deleteChildrenCmd = new CommandBuilder();
        foreach (var polylineE in node.Children.AsEnumerable().Reverse())
        {
            if (polylineE.Has<StrokeSetting>())
                _deleteChildrenCmd.SetTarget(polylineE).DeleteStroke();
            else
                _deleteChildrenCmd.SetTarget(polylineE).DeleteFilledPolygon();
        }

        _bodyHolder = layerE.Get<PolylineBodyHolder>();
        _layerView = layerE.Get<PolylineLayerView>();
    }

    public override void Do(Entity layerE)
    {
        // Delete children
        _deleteChildrenCmd.Do();

        // Cursor detection
        _bodyHolder.RemoveFromParent();
        layerE.Remove<PolylineBodyHolder>();

        // View
        _layerView.RemoveFromParent();
        layerE.Remove<PolylineLayerView>();

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
        worldView.InsertNodeAt(_layerView, _index); // order matters
        layerE.Add(_layerView);

        // Body
        var worldArea = Document.Get<WorldBody>();
        worldArea.AddChild(_bodyHolder);
        layerE.Add(_bodyHolder);

        // Restore Children
        _deleteChildrenCmd.Undo();
    }
}