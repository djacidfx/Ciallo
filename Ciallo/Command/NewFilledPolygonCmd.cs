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
    private readonly FilledPolygonSetting _setting;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewFilledPolygonCmd(FilledPolygonSetting setting = null)
    {
        _setting = setting ?? new FilledPolygonSetting();
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);
        targetE.Add(new PolylineGeometry());
        targetE.Add(_setting);

        // View
        var polygonView = new Polygon2D() { Antialiased = true }; // The antialiasing result is not satisfying
        targetE.AddNode(polygonView);
        _setting.Color.Subscribe(polygonView.SetColor).AddTo(targetE);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        targetE.AddNode(overlay);

        // Body
        var polygonBody = new Body();
        targetE.AddNode(polygonBody);

        // Layer tree events
        layerNode.TreeEntered.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);
            OnAdd(layerE, index);
        }).AddTo(targetE);

        layerNode.TreeExited.Subscribe(_ => OnRemove()).AddTo(targetE);

        layerNode.Moved.Subscribe(et =>
        {
            OnRemove();
            OnAdd(et.Value, et.NewIndex);
        }).AddTo(targetE);

        return;

        void OnAdd(Entity layerE, int index)
        {
            // View
            var layerView = layerE.Get<ShapeLayerView>();
            layerView.InsertNodeAt(polygonView, index);
            polygonView.SetOwner(layerView.Owner);

            // Overlay
            layerE.Get<OverlayHolder>().InsertNodeAt(overlay, index);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(polygonBody, index);
        }

        void OnRemove()
        {
            // Body
            polygonBody.RemoveFromParent();

            // Overlay
            overlay.RemoveFromParent();

            // View
            polygonView.RemoveFromParent();
        }
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        Document.Get<SelectionManager>().SelectedShapes.Remove(targetE);

        targetE.Detach<ToSerializeTag>();
    }
}