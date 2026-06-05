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
    private Entity _fillBrush;

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _fillBrush = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
        bool hasBrush = !_fillBrush.IsNull;
        bool hasArr = WorkingLayer.Has<Arrangement>() && hasBrush;

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
            _fillPreview.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement>(), data.WorldPosition);
        }
    }

    public override void Moving(CursorMotionData data)
    {
        _markerPreview?.SetGeometry([data.WorldPosition], [MarkerRadius]);
        _fillPreview?.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement>(), data.WorldPosition);
    }

    public override void End(CursorButtonData data)
    {
        ClearPreview();
        if (!_fillBrush.IsNull)
        {
            var arr = WorkingLayer.Get<Arrangement>();
            var faceRid = arr.Query(data.WorldPosition);
            if (!faceRid.IsValid || arr.IsUnboundedFace(faceRid))
            {
                Clear();
                return;
            }

            new CommandBuilder(WorkingLayer.World.Create())
                .NewVectorFillMarker()
                .AddToLayerTree(WorkingLayer)
                .SetPolylineGeometry([data.WorldPosition], [MarkerRadius], [1.0f], [Vector2.Zero])
                .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, _fillBrush)
                .Commit();
        }

        Clear();
    }

    public override void Cancel() => Clear();

    public void ClearPreview()
    {
        _markerPreview?.Free();
        _markerPreview = null;
        _fillPreview?.Free();
        _fillPreview = null;
    }

    public void Clear()
    {
        ClearPreview();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _fillBrush = Entity.Null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}
