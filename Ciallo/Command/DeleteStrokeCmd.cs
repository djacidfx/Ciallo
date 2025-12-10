using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteStrokeCmd : CommandBase
{
    private StrokeView _strokeView;
    private PolylineWireframe _strokeOverlay;
    private CursorDetectionArea _strokeArea;
    private StrokeSetting _strokeSetting;

    private Entity _parentE; // layer entity
    private int _index;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects =>
        new List<GodotObject> { _strokeView, _strokeOverlay, _strokeArea };

    public override void Do(Entity strokeE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(strokeE);

        // Cursor detection
        _strokeArea ??= strokeE.Get<CursorDetectionArea>();
        _strokeArea.RemoveFromParent();
        strokeE.Remove<CursorDetectionArea>();

        // Overlay
        _strokeOverlay ??= strokeE.Get<PolylineWireframe>();
        _strokeOverlay.RemoveFromParent();
        strokeE.Remove<PolylineWireframe>();

        // View
        _strokeView ??= strokeE.Get<StrokeView>();
        _strokeView.RemoveFromParent();
        strokeE.Remove<StrokeView>();

        // Data
        _strokeSetting ??= strokeE.Get<StrokeSetting>();
        if (_parentE.IsNull) _parentE = strokeE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(strokeE);
        _parentE.Get<LayerTreeNode>().RemoveChild(strokeE);
        strokeE.Remove<StrokeSetting>();
        strokeE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.
    }

    public override void Undo(Entity strokeE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, strokeE);
        strokeE.Add(_strokeSetting);
        strokeE.Tag<ToSerializeTag>();

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(_strokeView, _index);
        strokeE.Add(_strokeView);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_strokeOverlay);
        strokeE.Add(_strokeOverlay);

        // Cursor detection
        var areaHolder = _parentE.Get<PolylineAreaHolder>();
        areaHolder.InsertNodeAt(_strokeArea, _index);
        strokeE.Add(_strokeArea);
    }
}