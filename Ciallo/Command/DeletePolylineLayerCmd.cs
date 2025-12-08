using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeletePolylineLayerCmd : CommandBase
{
    private Entity _layerE;
    private Entity _parentE;
    private int _index;

    private readonly CommandBase _deleteChildrenCmd;
    private readonly PolylineLayerView _layerView;
    private readonly PolylineAreaHolder _areaHolder;

    public DeletePolylineLayerCmd(Entity layerE)
    {
        // Hierarchy not implemented, always remove from root.
        _layerE = layerE;
        var node = _layerE.Get<LayerTreeNode>();
        _parentE = node.Parent;

        _deleteChildrenCmd = new EmptyCommand();
        foreach (var polylineE in node.Children.AsEnumerable().Reverse())
        {
            if (polylineE.Has<StrokeSetting>())
                _deleteChildrenCmd.Combine(new DeleteStrokeCmd(polylineE));
            else
                _deleteChildrenCmd.Combine(new DeleteFilledPolygonCmd(polylineE));
        }

        _layerView = _layerE.Get<PolylineLayerView>();
        _areaHolder = _layerE.Get<PolylineAreaHolder>();
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_layerE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _layerView, _areaHolder };

    public override void Do()
    {
        // Delete children
        _deleteChildrenCmd.DoAllCombination();

        // Cursor detection
        _areaHolder.RemoveFromParent();
        _layerE.Remove<PolylineAreaHolder>();

        // View
        _layerView.RemoveFromParent();
        _layerE.Remove<PolylineLayerView>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(_layerE);

        // Data
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(_layerE);
        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
        _layerE.Detach<ToSerializeTag>();
    }

    public override void Undo()
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, _layerE);
        _layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(_layerE, _index);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(_layerView, _index); // order matters
        _layerE.Add(_layerView);

        // Cursor detection
        var worldArea = Document.Get<WorldCursorDetectionArea>();
        worldArea.AddChild(_areaHolder);
        _layerE.Add(_areaHolder);

        // Restore Children
        _deleteChildrenCmd.UndoAllCombination();
    }
}