using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

public class NewFilledPolygonCmd : CommandBase
{
    private readonly Entity _layerE;
    private readonly FilledPolygonSetting _setting;
    private Entity _polygonE;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_polygonE);

    public NewFilledPolygonCmd(Entity layerE, FilledPolygonSetting setting = null)
    {
        _layerE = layerE;
        _setting = setting ?? new FilledPolygonSetting();
        InitEntity();
    }

    private CompositeDisposable _subs;

    public override void Do()
    {
        _subs = new();
        // Data
        _polygonE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(_polygonE);
        _polygonE.Add(_setting);

        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(polygonView);
        _polygonE.Add(polygonView);
        polygonView.SetOwner(layerView.Owner);

        _setting.Color.Subscribe(polygonView.SetColor).AddTo(_polygonE).AddTo(_subs);
        CommandManager.RegisterProperty(_setting.Color).AddTo(_polygonE).AddTo(_subs);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(overlay);
        _polygonE.Add(overlay);

        // Cursor detection
        var polygonArea = new CursorDetectionArea();
        _layerE.Get<PolylineAreaHolder>().AddChild(polygonArea);
        _polygonE.Add(polygonArea);
    }

    public override void Undo()
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(_polygonE);

        // Cursor detection
        _polygonE.Get<CursorDetectionArea>().QueueFree();
        _polygonE.Remove<CursorDetectionArea>();

        // Overlay
        _polygonE.Get<PolylineWireframe>().QueueFree();
        _polygonE.Remove<PolylineWireframe>();

        // View
        // Pitfall: godot cannot deal with polygons shape as arabic numerals '8'.
        _polygonE.Get<Polygon2D>().QueueFree();
        _polygonE.Remove<Polygon2D>();

        // Data
        _polygonE.Remove<FilledPolygonSetting>();
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        _polygonE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }

    public Entity InitEntity()
    {
        if (!_polygonE.IsNull) return _polygonE;
        _polygonE = WorkingWorld.Create();
        var node = new LayerTreeNode();
        _polygonE.Add(new PolylineGeometry());
        _polygonE.Add(node);
        return _polygonE;
    }
}