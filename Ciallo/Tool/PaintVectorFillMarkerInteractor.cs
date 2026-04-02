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
    private VectorFillMarkerView _preview;
    private List<Polygon2D> _fillPreviews = [];

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        var vectorFillBrushE = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
        _preview = new();
        var setting = vectorFillBrushE.Get<VectorFillBrushSetting>();
        _preview.Sprite.Texture = setting.MarkerTexture.Value;
        _preview.Sprite.Modulate = setting.MarkerColor.Value;
        WorkingLayer.Get<OverlayHolder>().AddChild(_preview);
        _preview.SetGeometry([data.WorldPosition], [MarkerRadius]);
    }

    public override void Moving(CursorMotionData data)
    {
        _preview.SetGeometry([data.WorldPosition], [MarkerRadius]);
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
                .SetObservableCollection(e => e.Get<VectorFillLayerSetting>().ReferenceLayers, layers => layers.Add(WorkingLayer))
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
        _preview?.QueueFree();
        _preview = null;
        _fillPreviews.ForEach(node => node.QueueFree());
        _fillPreviews.Clear();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}