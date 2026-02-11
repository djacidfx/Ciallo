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

    public override void BeforeFirstDo(Entity strokeE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        strokeE.Add(layerNode);
        strokeE.Add(new StrokeSetting());
        strokeE.Add(new PolylineGeometry());

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        strokeE.AddNode(strokeView);

        // Overlay
        var strokeWireframe = new PolylineWireframe() { Visible = false };
        strokeE.AddNode(strokeWireframe);

        // Body
        var strokeBody = new Body();
        strokeE.AddNode(strokeBody);

        // Layer tree events
        layerNode.TreeEntered.Subscribe(et =>
        {
            (int index, var layerE) = (et.Index, et.Value);

            OnAdd(layerE, index);
        }).AddTo(strokeE);

        layerNode.TreeExited.Subscribe(_ =>
        {
            OnRemove();
        }).AddTo(strokeE);

        layerNode.Moved.Subscribe(et =>
        {
            OnRemove();
            OnAdd(et.Value, et.NewIndex);
        });
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
            layerE.Get<ShapeBodyHolder>().InsertNodeAt(strokeBody, index);
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

    public override void Do(Entity strokeE)
    {
        strokeE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity strokeE)
    {
        strokeE.Detach<ToSerializeTag>();
    }
}