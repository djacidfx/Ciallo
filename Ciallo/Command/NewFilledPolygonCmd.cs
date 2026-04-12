using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFilledPolygonCmd : CommandBase
{
    public Entity CopyE { get; }

    public NewFilledPolygonCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var polylineGeometry = CopyE.IsNull
            ? new PolylineGeometry()
            : CopyE.Get<PolylineGeometry>().Clone();
        targetE.Add(polylineGeometry);

        var setting = CopyE.IsNull
            ? new FilledPolygonSetting()
            : CopyE.Get<FilledPolygonSetting>().Clone();
        targetE.Add(setting);

        // View
        var polygonView = new Polygon2D() { Antialiased = true }; // The antialiasing result is not satisfying
        targetE.AddNode(polygonView);
        setting.Color.Subscribe(polygonView.SetColor).AddTo(targetE);

        // Overlay
        var overlay = new PolylineWireframe() { Visible = false };
        targetE.AddNode(overlay);

        // Body
        var polygonBody = new Body();
        targetE.AddNode(polygonBody);

        polylineGeometry.Positions.Subscribe(p =>
        {
            polygonView.SetPolygon(p.AsSpan());
            overlay.SetGeometry(p);
            polygonBody.SetSimplePolygon(p);
        }).AddTo(targetE);

        // Layer tree events
        var events = layerNode.MovedAsAddedRemoved;
        events.Added.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);
            // View
            var layerView = layerE.Get<ShapeLayerView>();
            layerView.InsertNodeAt(polygonView, index);
            polygonView.SetOwner(layerView.Owner);

            // Overlay
            layerE.Get<OverlayHolder>().InsertNodeAt(overlay, index);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(polygonBody, index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Body
            polygonBody.RemoveFromParent();

            // Overlay
            overlay.RemoveFromParent();

            // View
            polygonView.RemoveFromParent();
        }).AddTo(targetE);
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