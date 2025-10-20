using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class NewStrokeCmd : CommandBase
{
    private Entity _layerE;
    public Entity StrokeE;
    private readonly List<Node> _refNodes = [];

    public NewStrokeCmd(Entity layerE)
    {
        _layerE = layerE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(StrokeE);
    public override IEnumerable<GodotObject> DoRefObjects => _refNodes;

    public override void Do()
    {
        // Creation
        InitEntity();

        // Data
        StrokeE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(StrokeE);
        StrokeE.Add<StrokeBrush>(new Entity());

        // View
        if (_refNodes.Count == 0)
            _refNodes.Add(new StrokeView()
            {
                Material = BrushMaterial.MissingBrushMaterial,
            });
        var strokeView = (StrokeView)_refNodes[0];
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(strokeView);
        StrokeE.Add(strokeView);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        if (_refNodes.Count == 1) _refNodes.Add(new StrokeCenterline() { Visible = false });
        var strokeOverlay = (StrokeCenterline)_refNodes[1];
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(strokeOverlay);
        StrokeE.Add(strokeOverlay);

        // Cursor detection
        var geom = StrokeE.Get<PolylineGeometry>();
        var strokeArea = WorldCursorDetectionArea.CreateStroke(geom.Points, geom.Radii);
        _layerE.Get<PolylineAreaHolder>().AddChild(strokeArea);
        StrokeE.Add(strokeArea);
    }

    public override void Undo()
    {
        // Cursor detection
        StrokeE.Get<CursorDetectionArea>().QueueFree();
        StrokeE.Remove<CursorDetectionArea>();

        // Overlay
        StrokeE.Remove<StrokeCenterline>();
        _refNodes[1].GetParent().RemoveChild(_refNodes[1]);

        // View
        StrokeE.Remove<StrokeView>();
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.RemoveChild(_refNodes[0]);

        // Data
        StrokeE.Remove<StrokeBrush>();
        _layerE.Get<LayerTreeNode>().RemoveChild(^1);
        StrokeE.Detach<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        if (!StrokeE.IsNull) return StrokeE;
        StrokeE = WorkingWorld.Create();
        var node = new LayerTreeNode();
        StrokeE.Add(new PolylineGeometry());
        StrokeE.Add(node);
        return StrokeE;
    }
}