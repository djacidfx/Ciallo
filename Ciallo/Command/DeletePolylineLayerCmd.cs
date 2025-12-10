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
    private Entity _parentE;
    private int _index;

    private CommandBuilder _deleteChildrenCmd;
    private PolylineLayerView _layerView;
    private PolylineAreaHolder _areaHolder;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => [_layerView, _areaHolder];

    public override void Do(Entity layerE)
    {
        // Delete children
        if (_deleteChildrenCmd == null)
        {
            var node = layerE.Get<LayerTreeNode>();
            _parentE = node.Parent;
            _deleteChildrenCmd = new();
            foreach (var polylineE in node.Children.AsEnumerable().Reverse())
            {
                if (polylineE.Has<StrokeSetting>())
                    _deleteChildrenCmd.SetTarget(polylineE).DeleteStroke();
                else
                    _deleteChildrenCmd.SetTarget(polylineE).DeleteFilledPolygon();
            }
        }

        _deleteChildrenCmd.Do();

        // Cursor detection
        _areaHolder ??= layerE.Get<PolylineAreaHolder>();
        _areaHolder.RemoveFromParent();
        layerE.Remove<PolylineAreaHolder>();

        // View
        _layerView ??= layerE.Get<PolylineLayerView>();
        _layerView.RemoveFromParent();
        layerE.Remove<PolylineLayerView>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(layerE);

        // Data
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);
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

        // Cursor detection
        var worldArea = Document.Get<WorldCursorDetectionArea>();
        worldArea.AddChild(_areaHolder);
        layerE.Add(_areaHolder);

        // Restore Children
        _deleteChildrenCmd.Undo();
    }
}