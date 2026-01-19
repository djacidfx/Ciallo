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
    private Body _polygonBody;
    private FilledPolygonSetting _polygonSetting;

    private Entity _parentE; // layer entity
    private int _index = -1;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => [_polygonView, _polygonOverlay, _polygonBody];

    protected override void BeforeFirstDo(Entity polygonE)
    {
        _polygonBody = polygonE.Get<Body>();
        _polygonOverlay = polygonE.Get<PolylineWireframe>();
        _polygonView = polygonE.Get<Polygon2D>();
        _polygonSetting = polygonE.Get<FilledPolygonSetting>();

        _parentE = polygonE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().FindPathTo(polygonE).Single();
    }

    protected override void Do(Entity polygonE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(polygonE);

        // Body
        _polygonBody.RemoveFromParent();
        polygonE.Remove<Body>();

        // Overlay
        _polygonOverlay.RemoveFromParent();
        polygonE.Remove<PolylineWireframe>();

        // View
        _polygonView.RemoveFromParent();
        polygonE.Remove<Polygon2D>();

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(polygonE);
        polygonE.Remove<FilledPolygonSetting>();
        polygonE.Detach<ToSerializeTag>();
        // geometry objects to be deleted with entity itself.
    }

    protected override void Undo(Entity polygonE)
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

        // Body
        var areaHolder = _parentE.Get<PolylineBodyHolder>();
        areaHolder.InsertNodeAt(_polygonBody, _index);
        polygonE.Add(_polygonBody);
    }
}