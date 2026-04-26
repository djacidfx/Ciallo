using System;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Tool;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillMarkerCmd : CommandBase
{
    public Entity CopyE { get; }

    public NewVectorFillMarkerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var polylineGeometry = CopyE.IsNull
            ? new PolylineGeometry()
            : CopyE.Get<PolylineGeometry>().Clone();
        targetE.Add(polylineGeometry);

        var setting = CopyE.IsNull
            ? new VectorFillMarkerSetting()
            : CopyE.Get<VectorFillMarkerSetting>().Clone();
        targetE.Add(setting);
        if (!setting.BrushE.Value.IsNull && setting.BrushE.Value.World != targetE.World)
            setting.BrushE.Value = default;

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

        // Include both parent change and structure change.
        Observable<Arrangement2D> changeArrObs = layerNode.Parent
            .Select(e => e.TryGet<Arrangement2D>())
            .Select(arr =>
            {
                var obs = Observable.Return(arr);
                if (arr != null)
                    obs = obs.Merge(arr.StructureChanged.Select(_ => arr));
                return obs;
            })
            .Switch();
        polylineGeometry.Positions.CombineLatest(changeArrObs, ValueTuple.Create)
            .ThrottleLastFrame(1)
            .Subscribe(tuple =>
            {
                var (positions, arr) = tuple;
                if (arr == null || positions.IsDefaultOrEmpty)
                    polygonView.Clear();
                else
                    polygonView.SetPolygonWithQueryResult(arr, positions[0]);
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

        setting.BrushE.Subscribe(brushE =>
        {
            marker.Stroke.Material = !brushE.TryHas<StrokeBrushMaterial>()
                ? AutoloadRendering.MissingStrokeBrushMaterial
                : brushE.Get<StrokeBrushMaterial>();
        }).AddTo(targetE);
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
        var events = layerNode.MovedAsAddedRemoved;
        events.Added.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);
            // View
            var layerView = layerE.Get<ShapeLayerView>();
            layerView.InsertNodeAt(polygonView, index);
            polygonView.SetOwner(layerView.Owner);

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