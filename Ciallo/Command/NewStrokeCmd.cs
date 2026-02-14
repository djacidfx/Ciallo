using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeCmd : CommandBase
{
    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);
        targetE.Add(new StrokeSetting());
        targetE.Add(new PolylineGeometry());

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        targetE.AddNode(strokeView);

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
            Document.Get<WorldOverlay>().AddChild(strokeWireframe);

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