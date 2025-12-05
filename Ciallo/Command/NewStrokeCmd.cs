using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

public class NewStrokeCmd : CommandBase
{
    private Entity _layerE;
    private Entity _strokeE = Entity.Null;

    public NewStrokeCmd(Entity layerE)
    {
        _layerE = layerE;
        InitEntity();
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_strokeE);

    public override void Do()
    {
        // Data
        _strokeE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(_strokeE);
        _strokeE.Add(new StrokeSetting());
        _strokeE.Add(new PolylineGeometry());

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(strokeView);
        _strokeE.Add(strokeView);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        var strokeOverlay = new PolylineWireframe() { Visible = false };
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(strokeOverlay);
        _strokeE.Add(strokeOverlay);

        // Cursor detection
        var strokeArea = new CursorDetectionArea();
        _layerE.Get<PolylineAreaHolder>().AddChild(strokeArea);
        _strokeE.Add(strokeArea);
    }

    public override void Undo()
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(_strokeE);

        // Cursor detection
        _strokeE.Get<CursorDetectionArea>().QueueFree();
        _strokeE.Remove<CursorDetectionArea>();

        // Overlay
        var strokeOverlay = _strokeE.Get<PolylineWireframe>();
        _strokeE.Remove<PolylineWireframe>();
        strokeOverlay.QueueFree();

        // View
        var strokeView = _strokeE.Get<StrokeView>();
        _strokeE.Remove<StrokeView>();
        strokeView.QueueFree();

        // Data
        _strokeE.Remove<PolylineGeometry>();
        _strokeE.Remove<StrokeSetting>();
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        _strokeE.Detach<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        if (!_strokeE.IsNull) return _strokeE;
        _strokeE = WorkingWorld.Create();
        var node = new LayerTreeNode();
        _strokeE.Add(node);
        return _strokeE;
    }
}