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

    private CompositeDisposable _subs;

    public NewFilledPolygonCmd(Entity layerE, FilledPolygonSetting setting = null)
    {
        _layerE = layerE;
        _setting = setting ?? new FilledPolygonSetting();
    }

    public override void BeforeFirstDo(Entity polygonE)
    {
        polygonE.Add(new LayerTreeNode());
        polygonE.Add(new PolylineGeometry());
        polygonE.Add(_setting);
        _setting.RegisterProperties(CommandManager).AddTo(polygonE);

        // View
        var polygonView = new Polygon2D() { Antialiased = true }; // The antialiasing result is not satisfying
        polygonE.AddNode(polygonView);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        polygonE.AddNode(overlay);

        // Body
        var polygonBody = new Body();
        polygonE.AddNode(polygonBody);
    }

    public override void Do(Entity targetE)
    {
        _subs = new();
        _subs.AddTo(targetE);

        // Data
        targetE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(targetE);

        // View
        var polygonView = targetE.Get<Polygon2D>();
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(polygonView);
        polygonView.SetOwner(layerView.Owner);

        _setting.Color.Subscribe(polygonView.SetColor).AddTo(_subs);

        // Overlay
        var overlay = targetE.Get<PolylineWireframe>();
        Document.Get<WorldOverlay>().AddChild(overlay);

        // Body
        var polygonBody = targetE.Get<Body>();
        _layerE.Get<PolylineBodyHolder>().AddChild(polygonBody);
    }

    public override void Undo(Entity targetE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(targetE);

        // Body
        targetE.Get<Body>().RemoveFromParent();

        // Overlay
        targetE.Get<PolylineWireframe>().RemoveFromParent();

        // View
        targetE.Get<Polygon2D>().RemoveFromParent();

        // Data
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        targetE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }
}