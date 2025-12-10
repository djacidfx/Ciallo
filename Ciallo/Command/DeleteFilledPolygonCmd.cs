using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteFilledPolygonCmd : CommandBase
{
    private Polygon2D _polygonView;
    private PolylineWireframe _polygonOverlay;
    private CursorDetectionArea _polygonArea;
    private FilledPolygonSetting _polygonSetting;

    private Entity _parentE; // layer entity
    private int _index = -1;
    
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => [_polygonView, _polygonOverlay, _polygonArea];

    public override void Do(Entity polygonE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(polygonE);

        // Cursor detection
        _polygonArea ??= polygonE.Get<CursorDetectionArea>();
        _polygonArea.RemoveFromParent();
        polygonE.Remove<CursorDetectionArea>();

        // Overlay
        _polygonOverlay ??= polygonE.Get<PolylineWireframe>();
        _polygonOverlay.RemoveFromParent();
        polygonE.Remove<PolylineWireframe>();

        // View
        _polygonView ??= polygonE.Get<Polygon2D>();
        _polygonView.RemoveFromParent();
        polygonE.Remove<Polygon2D>();

        // Data
        _polygonSetting ??= polygonE.Get<FilledPolygonSetting>();
        if(_parentE.IsNull) _parentE = polygonE.Get<LayerTreeNode>().Parent;
        if(_index == -1) _index = _parentE.Get<LayerTreeNode>().FindPathTo(polygonE).Single();
        _parentE.Get<LayerTreeNode>().RemoveChild(polygonE);
        polygonE.Remove<FilledPolygonSetting>();
        polygonE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.
    }

    public override void Undo(Entity polygonE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, polygonE);
        polygonE.Add(_polygonSetting);
        polygonE.Tag<ToSerializeTag>();

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(_polygonView, _index);
        polygonE.Add(_polygonView);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_polygonOverlay);
        polygonE.Add(_polygonOverlay);

        // Cursor detection
        var areaHolder = _parentE.Get<PolylineAreaHolder>();
        areaHolder.InsertNodeAt(_polygonArea, _index);
        polygonE.Add(_polygonArea);
    }
}