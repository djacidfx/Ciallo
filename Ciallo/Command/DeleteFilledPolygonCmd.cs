using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class DeleteFilledPolygonCmd : CommandBase
{
    private Entity _polygonE;

    private readonly Polygon2D _polygonView;
    private readonly PolylineWireframe _polygonOverlay;
    private readonly CursorDetectionArea _polygonArea;
    private readonly FilledPolygonSetting _polygonSetting;

    private Entity _parentE; // layer entity
    private int _index;

    public DeleteFilledPolygonCmd(Entity polygonE)
    {
        _polygonE = polygonE;
        _polygonView = polygonE.Get<Polygon2D>();
        _polygonOverlay = polygonE.Get<PolylineWireframe>();
        _polygonArea = polygonE.Get<CursorDetectionArea>();
        _polygonSetting = polygonE.Get<FilledPolygonSetting>();
        _parentE = _polygonE.Get<LayerTreeNode>().Parent;
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_polygonE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _polygonView, _polygonOverlay, _polygonArea };

    public override void Do()
    {
        // Cursor detection
        _polygonArea.RemoveFromParent();
        _polygonE.Remove<CursorDetectionArea>();

        // Overlay
        _polygonOverlay.RemoveFromParent();
        _polygonE.Remove<PolylineWireframe>();

        // View
        _polygonView.RemoveFromParent();
        _polygonE.Remove<Polygon2D>();

        // Data
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(_polygonE);
        _parentE.Get<LayerTreeNode>().RemoveChild(_polygonE);
        _polygonE.Remove<FilledPolygonSetting>();
        _polygonE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.
    }

    public override void Undo()
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, _polygonE);
        _polygonE.Add(_polygonSetting);
        _polygonE.Tag<ToSerializeTag>();

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(_polygonView, _index);
        _polygonE.Add(_polygonView);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_polygonOverlay);
        _polygonE.Add(_polygonOverlay);

        // Cursor detection
        var areaHolder = _parentE.Get<PolylineAreaHolder>();
        areaHolder.InsertNodeAt(_polygonArea, _index);
        _polygonE.Add(_polygonArea);
    }
}