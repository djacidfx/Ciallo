using System.Collections.Immutable;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PaintFillInteractor : InteractiveSessionBase
{
    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Fixed,
        FixedRadius = AppPreference.StrokeWireframeRadius,
        AllowIntersection = false,
    };
    private StrokeView _dashPreview;
    private Color _fillColor;

    public override void BeforeTransitionSrcEnd(InteractiveSessionBase session)
    {
        _fillColor = ((PaintFillHover)session).FillColor.Value;
    }

    public override void Start(CursorButtonData data)
    {
        _generator.Start(data);

        _dashPreview = new StrokeView();
        _dashPreview.Material = AutoloadRendering.DashWireframeMaterial;
        var layerView = WorkingLayer.Get<ShapeLayerView>();
        layerView.AddChild(_dashPreview);
    }

    public override void Moving(CursorMotionData data)
    {
        _generator.Update(data);
        ImmutableArray<Vector2> points = [.._generator.Positions, _generator.Positions[0]];
        _dashPreview.SetGeometry(points, AppPreference.StrokeWireframeRadius);
    }

    public override void End(CursorButtonData data)
    {
        _generator.End(data);
        if (_generator.Positions.Count < 3)
        {
            Clear();
            return;
        }
        new CommandBuilder(WorkingLayer.World.Create())
            .NewFilledPolygon()
            .AddToLayerTree(WorkingLayer)
            .SetPolylineGeometry([.._generator.Positions], [.._generator.Radii], [.._generator.Pressures], [.._generator.Tilts])
            .SetProperty(e => e.Get<FilledPolygonSetting>().Color, _fillColor)
            .Commit();
        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    public void Clear()
    {
        _generator.Clear();
        _dashPreview?.QueueFree();
        _dashPreview = null;
    }
}