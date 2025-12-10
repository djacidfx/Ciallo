using System.Collections.Immutable;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PaintFillInteractor(PaintFillTool tool) : InteractorBase
{
    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Fixed,
        FixedRadius = AppPreference.StrokeWireframeRadius,
        AllowIntersection = false,
    };
    private StrokeView _dashPreview;

    public override bool Prepare(CursorButtonData data)
    {
        var l = SelectionManager.WorkingLayer.Value;
        return !l.IsDeletedOrNull() && l.Has<PolylineLayerSetting>();
    }

    public override void Start(CursorButtonData data)
    {
        _generator.Start(data);

        _dashPreview = new StrokeView();
        _dashPreview.Material = AutoloadRendering.DashWireframeMaterial;
        var layerE = SelectionManager.WorkingLayer.Value;
        var layerView = layerE.Get<PolylineLayerView>();
        layerView.AddChild(_dashPreview);
    }

    public override void Interacting(CursorMotionData data)
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
        var layerE = SelectionManager.WorkingLayer.Value;
        var setting = new FilledPolygonSetting() { Color = { Value = tool.Color.Value } };
        var geom = new PolylineGeometry()
        {
            Positions = [.._generator.Positions],
            Radii = [.._generator.Radii],
            Pressures = [.._generator.Pressures],
            Tilts = [.._generator.Tilts],
        };
        new CommandBuilder(layerE.World.Create())
            .NewFilledPolygon(layerE, setting)
            .SetPolylineGeometry(geom)
            .Commit();
        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        _generator.Clear();
        _dashPreview?.QueueFree();
        _dashPreview = null;
    }
}