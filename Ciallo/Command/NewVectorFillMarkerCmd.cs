using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
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

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

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
            ? new FillMarkerSetting()
            : CopyE.Get<FillMarkerSetting>().Clone();
        targetE.Add(setting);
        if (!setting.MarkerBrushE.Value.IsNull && setting.MarkerBrushE.Value.World != targetE.World)
            setting.MarkerBrushE.Value = default;

        // By design, polygons attached to fill markers are views,
        // Strokes attached are overlay
        // View
        var polygonView = new Polygon2D() { Antialiased = true };
        targetE.AddNode(polygonView);
        setting.MarkerBrushE
            .Where(e => !e.IsNull)
            .Select(e => e.Get<FillMarkerBrushSetting>().FillColor.AsObservable())
            .Switch()
            .Subscribe(polygonView.SetColor)
            .AddTo(targetE);

        // Overlay
        var wireframeOverlay = new PolylineWireframe() { Visible = false };
        targetE.AddNode(wireframeOverlay);

        polylineGeometry.Positions.DebounceFrame(1).Subscribe(p =>
        {
            wireframeOverlay.SetGeometry(p);
        }).AddTo(targetE);

        var strokeOverlay = new StrokeView() { Material = AutoloadRendering.MissingBrushMaterial };
        targetE.AddNode(strokeOverlay);

        setting.MarkerBrushE.Subscribe(brushE =>
        {
            strokeOverlay.Material = brushE.IsNull || !brushE.Has<BrushMaterial>()
                ? AutoloadRendering.MissingBrushMaterial
                : brushE.Get<BrushMaterial>();
        }).AddTo(targetE);

        polylineGeometry.ObserveAll().Subscribe(v =>
        {
            strokeOverlay.SetGeometry(v.Item1, v.Item2, v.Item3);
        }).AddTo(targetE);

        // Body
        var strokeBody = new Body();
        targetE.AddNode(strokeBody);

        polylineGeometry.ObserveShape().Subscribe(v =>
        {
            strokeBody.SetStrokeShape(v.Item1, v.Item2);
        }).AddTo(targetE);

        // Layer tree events
        var events = layerNode.MovedAsExitEnter;
        events.Enter.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);
            // View
            var layerView = layerE.Get<ShapeLayerView>();
            layerView.InsertNodeAt(polygonView, index);
            polygonView.SetOwner(layerView.Owner);

            // Overlay
            var holder = layerE.Get<OverlayHolder>();
            holder.GetChild(0).AddChild(strokeOverlay);
            holder.GetChild(1).AddChild(wireframeOverlay);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(strokeBody, index);
        }).AddTo(targetE);

        events.Exit.Subscribe(_ =>
        {
            // Body
            strokeBody.RemoveFromParent();

            // Overlay
            strokeOverlay.RemoveFromParent();
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