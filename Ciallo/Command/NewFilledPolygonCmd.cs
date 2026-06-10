using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFilledPolygonCmd : NewShapeCmdBase
{
    public NewFilledPolygonCmd(Entity copyE = default, IReadOnlyDictionary<Entity, Entity> entityMap = null)
        : base(copyE, entityMap)
    {
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    protected override void AddDataComponents(Entity targetE)
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
        setting.BrushE.Value = MapEntityRef(setting.BrushE.Value);
        targetE.Add(setting);
    }

    protected override void CreateRuntime(Entity targetE)
    {
        var layerNode = targetE.Get<LayerTreeNode>();
        var polylineGeometry = targetE.Get<PolylineGeometry>();
        var setting = targetE.Get<FilledPolygonSetting>();

        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        targetE.AddNode(polygonView);
        setting.BrushE
            .Select(e => e.IsNull
                ? Observable.Return(Colors.White)
                : e.Get<VectorFillBrushSetting>().FillColor.AsObservable())
            .Switch()
            .Subscribe(polygonView.SetColor)
            .AddTo(targetE);
        setting.BrushE.Subscribe(brushE =>
        {
            polygonView.Material = brushE.IsNull ? AutoloadRendering.MissingFillBrushMaterial : null;
            polygonView.Texture = brushE.IsNull ? AutoloadRendering.DummyTextureForUV : null;
        }).AddTo(targetE);

        polylineGeometry.Positions.Subscribe(ps =>
        {
            polygonView.SetPolygonFromRawRing(ps);
        }).AddTo(targetE);

        // Overlay & Body
        var overlay = new PolylineWireframe() { Visible = false };
        targetE.AddNode(overlay);
        var polygonBody = new Body();
        targetE.AddNode(polygonBody);
        polylineGeometry.Positions.Subscribe(ps =>
        {
            overlay.SetGeometry(ps);
            polygonBody.SetPolygonFromRawRing(ps);
        }).AddTo(targetE);

        // Layer tree events
        var events = layerNode.MovedReparentedAsAddedRemoved;
        events.Added.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Parent);
            // View
            var layerView = layerE.Get<ShapeLayerView>();
            layerView.InsertNodeAt(polygonView, index);
            polygonView.SetOwner(Document.Get<WorldView>());

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
