using System;
using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Tool;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillMarkerCmd : NewShapeCmdBase
{
    public NewVectorFillMarkerCmd(Entity copyE = default, IReadOnlyDictionary<Entity, Entity> entityMap = null)
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
            ? new VectorFillMarkerSetting()
            : CopyE.Get<VectorFillMarkerSetting>().Clone();
        setting.BrushE.Value = MapEntityRef(setting.BrushE.Value);
        targetE.Add(setting);
    }

    protected override void CreateRuntime(Entity targetE)
    {
        var layerNode = targetE.Get<LayerTreeNode>();
        var polylineGeometry = targetE.Get<PolylineGeometry>();
        var setting = targetE.Get<VectorFillMarkerSetting>();

        // By design, polygons attached to fill markers are views,
        // Strokes and marker sprites attached are overlays.
        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        targetE.AddNode(polygonView);
        setting.BrushE
            .Select(e => e.IsNull
                ? Observable.Return(Colors.Black)
                : e.Get<VectorFillBrushSetting>().FillColor.AsObservable())
            .Switch()
            .Subscribe(polygonView.SetColor)
            .AddTo(targetE);
        setting.BrushE.Subscribe(brushE =>
        {
            polygonView.Material = brushE.IsNull ? AutoloadRendering.MissingFillBrushMaterial : null;
            polygonView.Texture = brushE.IsNull ? AutoloadRendering.DummyTextureForUV : null;
        }).AddTo(targetE);

        // Polygon view — ArrReady emits whenever the arrangement is settled and safe to query.
        layerNode.Parent
            .Select(e => e.IsNull
                ? Observable.Return<Arrangement>(null)
                : e.Get<ArrangementManager>().ArrReady.AsObservable())
            .Switch()
            .CombineLatest(polylineGeometry.Positions.ThrottleLastFrame(1), ValueTuple.Create)
            .Subscribe(tuple =>
            {
                var (arr, positions) = tuple;
                if (positions.IsDefaultOrEmpty)
                    polygonView.Clear();
                else if (arr != null)
                    polygonView.SetPolygonWithQueryResult(arr, positions[0]);
                // else: arr mid-rebuild — keep last frame's polygon to avoid flicker.
            }).AddTo(targetE);

        // Overlay
        var wireframeOverlay = new PolylineWireframe() { Visible = false };
        targetE.AddNode(wireframeOverlay);

        polylineGeometry.Positions.ThrottleLastFrame(1).Subscribe(p =>
        {
            wireframeOverlay.SetGeometry(p);
        }).AddTo(targetE);

        var marker = new VectorFillMarkerView();
        targetE.AddNode(marker);

        setting.BrushE
            .Select(e => e.IsNull
                ? Observable.Return<ImageTexture>(null)
                : e.Get<VectorFillBrushSetting>().MarkerTexture.AsObservable())
            .Switch()
            .Subscribe(marker.Sprite.SetTexture)
            .AddTo(targetE);
        setting.BrushE
            .Select(e => e.IsNull
                ? Observable.Return(Colors.Black)
                : e.Get<VectorFillBrushSetting>().MarkerColor.AsObservable())
            .Switch()
            .Subscribe(marker.Sprite.SetModulate)
            .AddTo(targetE);

        polylineGeometry.ObserveShape().ThrottleLastFrame(1).Subscribe(v =>
        {
            marker.SetGeometry(v.Item1, v.Item2);
        }).AddTo(targetE);

        // Body
        var strokeBody = new Body();
        targetE.AddNode(strokeBody);

        polylineGeometry.ObserveShape().ThrottleLastFrame(1).Subscribe(v =>
        {
            strokeBody.SetStrokeShape(v.Item1, v.Item2);
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
            var holder = layerE.Get<OverlayHolder>();
            holder.GetChild(0).AddChild(marker);
            holder.GetChild(1).AddChild(wireframeOverlay);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(strokeBody, index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Body
            strokeBody.RemoveFromParent();

            // Overlay
            marker.RemoveFromParent();
            wireframeOverlay.RemoveFromParent();

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
