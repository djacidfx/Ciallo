using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFilledPolygonCmd : CommandBase
{
    private readonly Entity _layerE;
    private readonly FilledPolygonSetting _setting;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewFilledPolygonCmd(Entity layerE, FilledPolygonSetting setting = null)
    {
        _layerE = layerE;
        _setting = setting ?? new FilledPolygonSetting();
    }

    private CompositeDisposable _subs;

    protected override void Do(Entity polygonE)
    {
        _subs = new();
        _subs.AddTo(polygonE);
        // Data
        if (!polygonE.Has<LayerTreeNode>())
        {
            polygonE.Add(new LayerTreeNode());
            polygonE.Add(new PolylineGeometry());
        }

        polygonE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(polygonE);
        polygonE.Add(_setting);

        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(polygonView);
        polygonE.Add(polygonView);
        polygonView.SetOwner(layerView.Owner);

        _setting.Color.Subscribe(polygonView.SetColor).AddTo(polygonE).AddTo(_subs);
        CommandManager.RegisterProperty(_setting.Color).AddTo(polygonE).AddTo(_subs);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(overlay);
        polygonE.Add(overlay);

        // Cursor detection
        var polygonArea = new CursorDetectionArea();
        _layerE.Get<PolylineAreaHolder>().AddChild(polygonArea);
        polygonE.Add(polygonArea);
    }

    protected override void Undo(Entity polygonE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(polygonE);

        // Cursor detection
        polygonE.Get<CursorDetectionArea>().QueueFree();
        polygonE.Remove<CursorDetectionArea>();

        // Overlay
        polygonE.Get<PolylineWireframe>().QueueFree();
        polygonE.Remove<PolylineWireframe>();

        // View
        // Pitfall: godot cannot deal with polygons shape as arabic numerals '8'.
        polygonE.Get<Polygon2D>().QueueFree();
        polygonE.Remove<Polygon2D>();

        // Data
        polygonE.Remove<FilledPolygonSetting>();
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        polygonE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }
}