using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class DeleteStrokeCmd : CommandBase
{
    private Entity _strokeE;

    private readonly StrokeView _strokeView;
    private readonly PolylineWireframe _strokeOverlay;
    private readonly CursorDetectionArea _strokeArea;
    private readonly StrokeBrush _strokeBrush;

    private readonly Entity _parentE; // layer entity
    private readonly int _index;

    public DeleteStrokeCmd(Entity strokeE)
    {
        _strokeE = strokeE;
        _strokeView = strokeE.Get<StrokeView>();
        _strokeOverlay = strokeE.Get<PolylineWireframe>();
        _strokeArea = strokeE.Get<CursorDetectionArea>();
        _strokeBrush = strokeE.Get<StrokeBrush>();
        _parentE = _strokeE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(_strokeE);
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_strokeE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _strokeView, _strokeOverlay, _strokeArea };

    public override void Do()
    {
        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(_strokeE);
        _strokeE.Remove<StrokeBrush>();
        _strokeE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.

        // View
        _strokeView.RemoveFromParent();
        _strokeE.Remove<StrokeView>();

        // Overlay
        _strokeOverlay.RemoveFromParent();
        _strokeE.Remove<PolylineWireframe>();

        // Cursor detection
        _strokeArea.RemoveFromParent();
        _strokeE.Remove<CursorDetectionArea>();
    }

    public override void Undo()
    {
        // Cursor detection
        var areaHolder = _parentE.Get<PolylineAreaHolder>();
        areaHolder.InsertNodeAt(_strokeArea, _index);
        _strokeE.Add(_strokeArea);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_strokeOverlay);
        _strokeE.Add(_strokeOverlay);

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(_strokeView, _index);
        _strokeE.Add(_strokeView);

        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, _strokeE);
        _strokeE.Add(_strokeBrush);
        _strokeE.Tag<ToSerializeTag>();
    }
}