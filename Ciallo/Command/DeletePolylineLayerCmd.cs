using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class DeletePolylineLayerCmd : CommandBase
{
    private Entity _layerE;
    private Entity _parentE;
    private readonly int _originalIndex;

    private readonly PolylineLayerView _layerView;
    private readonly PolylineAreaHolder _areaHolder;

    public DeletePolylineLayerCmd(Entity layerE)
    {
        // Hierarchy not implemented, always remove from root.
        _layerE = layerE;
        _originalIndex = Document.Get<LayerTreeNode>().FindPathTo(_layerE).Single();

        _layerView = _layerE.Get<PolylineLayerView>();
        _areaHolder = _layerE.Get<PolylineAreaHolder>();
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_layerE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _layerView, _areaHolder };

    public override void Do()
    {
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
        _parentE = _layerE.Get<LayerTreeNode>().Parent;
        _parentE.Get<LayerTreeNode>().RemoveChild(_originalIndex);
        _layerE.Detach<ToSerializeTag>();
    }

    public override void Undo()
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_originalIndex, _layerE);
        _layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.CreateInsert(_layerE, _originalIndex);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(_layerView, _originalIndex); // order matters
        _layerE.Add(_layerView);

        // Cursor detection
        var worldArea = Document.Get<WorldCursorDetectionArea>();
        worldArea.AddChild(_areaHolder);
        _layerE.Add(_areaHolder);
    }
}