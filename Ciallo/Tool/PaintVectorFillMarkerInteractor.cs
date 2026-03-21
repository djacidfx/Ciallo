using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintVectorFillMarkerInteractor : InteractiveSessionBase
{
    private StrokeView _strokePreview;
    private List<Polygon2D> _fillPreviews = [];
    private Entity _vectorFillBrushE;

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _vectorFillBrushE = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;

        _strokePreview = new StrokeView() { Material = _vectorFillBrushE.Get<StrokeBrushMaterial>() };
        WorkingLayer.Get<ShapeLayerView>().AddChild(_strokePreview);
        _strokePreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
    }

    public override void Moving(CursorMotionData data)
    {
        _strokePreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
    }

    public override void End(CursorButtonData data)
    {
        var cmd = new CommandBuilder();
        Entity parentE = WorkingLayer;
        if (WorkingLayer.Has<ShapeLayerSetting>())
        {
            parentE = WorkingLayer.World.Create();
            var i = WorkingLayer.Get<LayerTreeNode>().Index;
            cmd.SetTarget(parentE)
                .NewVectorFillLayer()
                .AddToLayerTree(Document, i)
                .SetWorkingLayer();
        }
        var brushE = WorkingLayer.Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
        cmd.SetTarget(WorkingLayer.World.Create())
            .NewVectorFillMarker()
            .AddToLayerTree(parentE)
            .SetPolylineGeometry([data.WorldPosition], [MarkerRadius], [1.0f], [Vector2.Zero])
            .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, brushE)
            .Commit();

        Clear();
    }

    public override void Cancel() => Clear();

    public void Clear()
    {
        _strokePreview.QueueFree();
        _strokePreview = null;
        _fillPreviews.ForEach(node => node.QueueFree());
        _fillPreviews.Clear();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }


    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}