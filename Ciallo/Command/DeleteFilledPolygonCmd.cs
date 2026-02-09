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
    private Entity _parentE; // layer entity
    private int _index = -1;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity polygonE)
    {
        _parentE = polygonE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().FindPathTo(polygonE).Single();
    }

    public override void Do(Entity polygonE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(polygonE);

        // Body
        polygonE.Get<Body>().RemoveFromParent();

        // Overlay
        polygonE.Get<PolylineWireframe>().RemoveFromParent();

        // View
        polygonE.Get<Polygon2D>().RemoveFromParent();

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(polygonE);
        polygonE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity polygonE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, polygonE);
        polygonE.Tag<ToSerializeTag>();

        // View
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(polygonE.Get<Polygon2D>(), _index);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(polygonE.Get<PolylineWireframe>());

        // Body
        var areaHolder = _parentE.Get<PolylineBodyHolder>();
        areaHolder.InsertNodeAt(polygonE.Get<Body>(), _index);
    }
}