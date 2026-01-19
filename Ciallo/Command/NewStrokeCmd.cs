using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeCmd : CommandBase
{
    private Entity _layerE;

    public NewStrokeCmd(Entity layerE)
    {
        _layerE = layerE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    protected override void BeforeFirstDo(Entity strokeE)
    {
        strokeE.Add(new LayerTreeNode());
        strokeE.Add(new StrokeSetting());
        strokeE.Add(new PolylineGeometry());
    }

    protected override void Do(Entity strokeE)
    {
        // Data
        strokeE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(strokeE);

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(strokeView);
        strokeE.Add(strokeView);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        var strokeOverlay = new PolylineWireframe() { Visible = false };
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(strokeOverlay);
        strokeE.Add(strokeOverlay);

        // Body
        var strokeBody = new Body();
        _layerE.Get<PolylineBodyHolder>().AddChild(strokeBody);
        strokeE.Add(strokeBody);
    }

    protected override void Undo(Entity strokeE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(strokeE);

        // Body
        strokeE.Get<Body>().QueueFree();
        strokeE.Remove<Body>();

        // Overlay
        var strokeOverlay = strokeE.Get<PolylineWireframe>();
        strokeE.Remove<PolylineWireframe>();
        strokeOverlay.QueueFree();

        // View
        var strokeView = strokeE.Get<StrokeView>();
        strokeE.Remove<StrokeView>();
        strokeView.QueueFree();

        // Data
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        strokeE.Detach<ToSerializeTag>();
    }
}