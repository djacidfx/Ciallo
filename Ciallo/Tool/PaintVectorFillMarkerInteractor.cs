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
    private Entity _fillMakerBrushE;

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _fillMakerBrushE = Document.Get<SelectionManager>().WorkingMarkerBrush.Value;

        _strokePreview = new StrokeView() { Material = _fillMakerBrushE.Get<BrushMaterial>() };
        WorkingLayer.Get<ShapeLayerView>().AddChild(_strokePreview);
        _strokePreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
    }

    public override void Moving(CursorMotionData data)
    {
        _strokePreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
    }

    public override void End(CursorButtonData data)
    {
        new CommandBuilder(WorkingLayer.World.Create())
            .NewFillMarker()
            .AddToLayerTree(WorkingLayer)
            .SetPolylineGeometry([data.WorldPosition], [MarkerRadius], [1.0f], [Vector2.Zero])
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