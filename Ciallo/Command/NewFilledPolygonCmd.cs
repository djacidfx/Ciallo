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
    public Entity PolygonE { get; private set; }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(PolygonE);

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
        PolygonE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(PolygonE);
        PolygonE.Add(_setting);

        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(polygonView);
        PolygonE.Add(polygonView);
        polygonView.SetOwner(layerView.Owner);

        _setting.Color.Subscribe(polygonView.SetColor).AddTo(PolygonE).AddTo(_subs);
        CommandManager.RegisterProperty(_setting.Color).AddTo(PolygonE).AddTo(_subs);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(overlay);
        PolygonE.Add(overlay);

        // Cursor detection
        var polygonArea = new CursorDetectionArea();
        _layerE.Get<PolylineAreaHolder>().AddChild(polygonArea);
        PolygonE.Add(polygonArea);
    }

    public override void Undo()
    {
        // Cursor detection
        PolygonE.Get<CursorDetectionArea>().QueueFree();
        PolygonE.Remove<CursorDetectionArea>();

        // Overlay
        PolygonE.Get<PolylineWireframe>().QueueFree();
        PolygonE.Remove<PolylineWireframe>();

        // View
        // Pitfall: godot cannot deal with polygons shape as arabic numerals '8'.
        PolygonE.Get<Polygon2D>().QueueFree();
        PolygonE.Remove<Polygon2D>();

        // Data
        PolygonE.Remove<FilledPolygonSetting>();
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        PolygonE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }

    public Entity InitEntity()
    {
        if (!PolygonE.IsNull) return PolygonE;
        PolygonE = WorkingWorld.Create();
        var node = new LayerTreeNode();
        PolygonE.Add(new PolylineGeometry());
        PolygonE.Add(node);
        return PolygonE;
    }
}