using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteStrokeCmd : CommandBase
{
    private Entity _strokeE;

    private readonly StrokeView _strokeView;
    private readonly PolylineWireframe _strokeOverlay;
    private readonly CursorDetectionArea _strokeArea;
    private readonly StrokeSetting _strokeSetting;

    private readonly Entity _parentE; // layer entity
    private int _index;

    public DeleteStrokeCmd(Entity strokeE)
    {
        _strokeE = strokeE;
        _strokeView = strokeE.Get<StrokeView>();
        _strokeOverlay = strokeE.Get<PolylineWireframe>();
        _strokeArea = strokeE.Get<CursorDetectionArea>();
        _strokeSetting = strokeE.Get<StrokeSetting>();
        _parentE = _strokeE.Get<LayerTreeNode>().Parent;
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_strokeE);
    public override IEnumerable<GodotObject> UndoRefObjects =>
        new List<GodotObject> { _strokeView, _strokeOverlay, _strokeArea };

    public override void Do()
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(_strokeE);

        // Cursor detection
        _strokeArea.RemoveFromParent();
        _strokeE.Remove<CursorDetectionArea>();

        // Overlay
        _strokeOverlay.RemoveFromParent();
        _strokeE.Remove<PolylineWireframe>();

        // View
        _strokeView.RemoveFromParent();
        _strokeE.Remove<StrokeView>();

        // Data
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(_strokeE);
        _parentE.Get<LayerTreeNode>().RemoveChild(_strokeE);
        _strokeE.Remove<StrokeSetting>();
        _strokeE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.
    }

    public override void Undo()
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, _strokeE);
        _strokeE.Add(_strokeSetting);
        _strokeE.Tag<ToSerializeTag>();

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(_strokeView, _index);
        _strokeE.Add(_strokeView);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_strokeOverlay);
        _strokeE.Add(_strokeOverlay);

        // Cursor detection
        var areaHolder = _parentE.Get<PolylineAreaHolder>();
        areaHolder.InsertNodeAt(_strokeArea, _index);
        _strokeE.Add(_strokeArea);
    }
}