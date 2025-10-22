using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

public class NewStrokeCmd : CommandBase
{
    private Entity _layerE;
    public Entity StrokeE { get; private set; } = Entity.Null;

    public NewStrokeCmd(Entity layerE)
    {
        _layerE = layerE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(StrokeE);

    public override void Do()
    {
        // Creation
        InitEntity();

        // Data
        StrokeE.Tag<ToSerializeTag>();
        _layerE.Get<LayerTreeNode>().AddChild(StrokeE);
        StrokeE.Add<StrokeBrush>(Entity.Null);

        // View
        var strokeView = new StrokeView()
        {
            Material = BrushMaterial.MissingBrushMaterial,
        };
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.AddChild(strokeView);
        StrokeE.Add(strokeView);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        var strokeOverlay = new StrokeCenterline() { Visible = false };
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
        var strokeOverlay = StrokeE.Get<StrokeCenterline>();
        StrokeE.Remove<StrokeCenterline>();
        strokeOverlay.QueueFree();

        // View
        var strokeView = StrokeE.Get<StrokeView>();
        StrokeE.Remove<StrokeView>();
        strokeView.QueueFree();

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