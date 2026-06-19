using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeCmd : NewShapeCmdBase
{
    public override void OnDeletedAsDo() => TargetE.Delete();

    public NewStrokeCmd(Entity copyE = default, IReadOnlyDictionary<Entity, Entity> entityMap = null)
        : base(copyE, entityMap) { }

    protected override void AddDataComponents(Entity targetE)
    {
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var strokeSetting = CopyE.IsNull
            ? new StrokeSetting()
            : CopyE.Get<StrokeSetting>().Clone();
        strokeSetting.BrushE.Value = MapEntityRef(strokeSetting.BrushE.Value);
        targetE.Add(strokeSetting);

        var polylineGeometry = CopyE.IsNull
            ? new PolylineGeometry()
            : CopyE.Get<PolylineGeometry>().Clone();
        targetE.Add(polylineGeometry);
    }

    protected override void CreateRuntime(Entity targetE)
    {
        var layerNode = targetE.Get<LayerTreeNode>();
        var strokeSetting = targetE.Get<StrokeSetting>();
        var polylineGeometry = targetE.Get<PolylineGeometry>();

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingStrokeBrushMaterial,
        };
        targetE.AddNode(strokeView);

        strokeSetting.BrushE.Subscribe(brushE =>
        {
            strokeView.Material = !brushE.TryHas<StrokeBrushMaterial>()
                ? AutoloadRendering.MissingStrokeBrushMaterial
                : brushE.Get<StrokeBrushMaterial>();
        }).AddTo(targetE);

        polylineGeometry.ObserveAll().ThrottleLastFrame(1).Subscribe(v =>
        {
            strokeView.SetGeometry(v.Item1, v.Item2, v.Item3);
        }).AddTo(targetE);

        // Overlay
        var strokeWireframe = new PolylineWireframe() { Visible = false };
        targetE.AddNode(strokeWireframe);

        polylineGeometry.Positions.ThrottleLastFrame(1).Subscribe(p =>
        {
            strokeWireframe.SetGeometry(p);
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
            layerView.InsertNodeAt(strokeView, index);

            // Overlay
            layerE.Get<OverlayHolder>().InsertNodeAt(strokeWireframe, index);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(strokeBody, index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Body
            strokeBody.RemoveFromParent();

            // Overlay
            strokeWireframe.RemoveFromParent();

            // View
            strokeView.RemoveFromParent();
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