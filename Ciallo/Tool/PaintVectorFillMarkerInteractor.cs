using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintVectorFillMarkerInteractor : InteractiveSessionBase
{
    private VectorFillMarkerView _markerPreview;
    private Polygon2D _fillPreview;
    private Rid _previewFaceId;
    private Entity _fillBrush;

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _fillBrush = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
        bool hasBrush = !_fillBrush.IsNull;
        bool hasArr = WorkingLayer.Has<Arrangement2D>() && hasBrush;

        if (!hasBrush) return;
        // To preview marker
        _markerPreview = new();
        var setting = _fillBrush.Get<VectorFillBrushSetting>();
        _markerPreview.Sprite.Texture = setting.MarkerTexture.Value;
        _markerPreview.Sprite.Modulate = setting.MarkerColor.Value;
        WorkingLayer.Get<OverlayHolder>().AddChild(_markerPreview);
        _markerPreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
        if (hasArr)
        {
            // To preview fill
            _fillPreview = new() { Color = setting.FillColor.Value };
            WorkingLayer.Get<ShapeLayerView>().AddChild(_fillPreview);
            UpdateFillPreview(data.WorldPosition);
        }
    }

    public override void Moving(CursorMotionData data)
    {
        _markerPreview?.SetGeometry([data.WorldPosition], [MarkerRadius]);
        UpdateFillPreview(data.WorldPosition);
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
        if (!_fillBrush.IsNull)
        {
            cmd.SetTarget(WorkingLayer.World.Create())
                .NewVectorFillMarker()
                .AddToLayerTree(parentE)
                .SetPolylineGeometry([data.WorldPosition], [MarkerRadius], [1.0f], [Vector2.Zero])
                .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, _fillBrush);
        }
        cmd.Commit();
        Clear();
    }

    public override void Cancel() => Clear();

    public void Clear()
    {
        _markerPreview?.QueueFree();
        _markerPreview = null;
        _fillPreview?.QueueFree();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _previewFaceId = default;
        _fillBrush = Entity.Null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    private void UpdateFillPreview(Vector2 position)
    {
        if (_fillPreview == null) return;

        var arr = WorkingLayer.Get<Arrangement2D>();
        var faceId = arr.Query(position);
        if (faceId == _previewFaceId)
            return;

        _previewFaceId = faceId;

        var polygons = arr.GetPolygonWithHoles(faceId);
        _fillPreview.Polygon = polygons.Count > 0 ? polygons.First() : [];
    }
}