using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintVectorFillMarkerInteractor : InteractiveSessionBase
{
    private VectorFillMarkerView _markerPreview;
    private Polygon2D _fillPreview;
    private Entity _fillBrush;

    private Label _lackOfBrushLabel;

    private float MarkerRadius => AppPreference.VectorFillMarkerRadius.Value;

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _fillBrush = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
        bool hasBrush = !_fillBrush.IsNull;

        if (!hasBrush)
        {
            _lackOfBrushLabel.Visible = true;
            return;
        }
        // To preview marker
        _markerPreview = new();
        var setting = _fillBrush.Get<VectorFillBrushSetting>();
        _markerPreview.Sprite.Texture = setting.MarkerTexture.Value;
        _markerPreview.Sprite.Modulate = setting.MarkerColor.Value;
        WorkingLayer.Get<OverlayHolder>().AddChild(_markerPreview);
        _markerPreview.SetGeometry([data.WorldPosition], [MarkerRadius]);
        var arr = WorkingLayer.Get<ArrangementManager>().ArrReady.CurrentValue;
        _fillPreview = new() { Color = setting.FillColor.Value };
        WorkingLayer.Get<ShapeLayerView>().AddChild(_fillPreview);
        if (arr != null)
        {
            _fillPreview.SetPolygonWithQueryResult(arr, data.WorldPosition);
        }
    }

    public override void Moving(CursorMotionData data)
    {
        _markerPreview?.SetGeometry([data.WorldPosition], [MarkerRadius]);
        var arr = WorkingLayer.Get<ArrangementManager>().ArrReady.CurrentValue;
        if (arr == null) return;
        _fillPreview?.SetPolygonWithQueryResult(arr, data.WorldPosition);
    }

    public override void End(CursorButtonData data)
    {
        ClearPreview();
        // Allow to place marker even if the arrangement is not ready or fill on unbounded area or brush is null.
        new CommandBuilder(WorkingLayer.World.Create())
            .NewVectorFillMarker()
            .AddToLayerTree(WorkingLayer)
            .SetPolylineGeometry([data.WorldPosition], [MarkerRadius], [1.0f], [Vector2.Zero])
            .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, _fillBrush)
            .Commit();

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
        _lackOfBrushLabel.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _fillBrush = Entity.Null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    public override void DrawProperty(PropertyContainer container)
    {
        _lackOfBrushLabel = new Label()
        {
            Text = "[No Brush Hint]",
            Visible = false,
        };
        container.AddChild(_lackOfBrushLabel);
    }
}
