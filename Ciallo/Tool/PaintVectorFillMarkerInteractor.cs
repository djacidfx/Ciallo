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
    private VectorFillMarkerView _markerPreview;
    private Polygon2D _fillPreview;
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
            _fillPreview.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement2D>(), data.WorldPosition);
        }
    }

    public override void Moving(CursorMotionData data)
    {
        _markerPreview?.SetGeometry([data.WorldPosition], [MarkerRadius]);
        _fillPreview?.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement2D>(), data.WorldPosition);
    }

    public override void End(CursorButtonData data)
    {
        var cmd = new CommandBuilder();
        Entity parentE = WorkingLayer; // parent of marker
        if (WorkingLayer.Has<ShapeLayerSetting>())
        {
            parentE = WorkingLayer.World.Create();

            // Get all visible shape layers as reference layers
            List<Entity> referencesLayers = [];
            foreach (var layer in WorkingLayer.World.Query<ShapeLayerSetting>().EnumerateWithEntities())
            {
                if (layer.Tagged<ToSerializeTag>() &&
                    layer.Get<CommonLayerSetting>().IsVisible.Value)
                {
                    referencesLayers.Add(layer);
                }
            }

            cmd.SetTarget(parentE).NewVectorFillLayer();
            if (referencesLayers.Count > 0)
            {
                cmd.SetObservableCollection(e => e.Get<VectorFillLayerSetting>().ReferenceLayers,
                    layers => layers.AddRange(referencesLayers));
            }
            cmd.AddToLayerTree(Document).SetWorkingLayer();
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
        _fillBrush = Entity.Null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}