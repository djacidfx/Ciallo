using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeCmd : CommandBase
{
    public Entity CopyE { get; }
    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewStrokeCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var strokeSetting = CopyE.IsNull
            ? new StrokeSetting()
            : CopyE.Get<StrokeSetting>().Clone();
        targetE.Add(strokeSetting);

        var polylineGeometry = CopyE.IsNull
            ? new PolylineGeometry()
            : CopyE.Get<PolylineGeometry>().Clone();
        targetE.Add(polylineGeometry);

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        targetE.AddNode(strokeView);

        strokeSetting.BrushE.Subscribe(brushE =>
        {
            strokeView.Material = brushE.IsNull || !brushE.Has<BrushMaterial>()
                ? AutoloadRendering.MissingBrushMaterial
                : brushE.Get<BrushMaterial>();
        }).AddTo(targetE);

        // Overlay
        var strokeWireframe = new PolylineWireframe() { Visible = false };
        targetE.AddNode(strokeWireframe);

        // Body
        var strokeBody = new Body();
        targetE.AddNode(strokeBody);

        // Layer tree events
        layerNode.TreeEntered.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);

            OnAdd(layerE, index);
        }).AddTo(targetE);

        layerNode.TreeExited.Subscribe(_ =>
        {
            OnRemove();
        }).AddTo(targetE);

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
            layerView.InsertNodeAt(strokeView, index);
            strokeView.SetOwner(layerView.Owner);

            // Overlay
            layerE.Get<OverlayHolder>().InsertNodeAt(strokeWireframe, index);

            // Body
            layerE.Get<BodyHolder>().InsertNodeAt(strokeBody, index);
        }

        void OnRemove()
        {
            // Body
            strokeBody.RemoveFromParent();

            // Overlay
            strokeWireframe.RemoveFromParent();

            // View
            strokeView.RemoveFromParent();
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