using System.Collections.Immutable;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PaintFillInteractor(PaintFillTool tool) : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer.Value;
            return !l.IsNull && l.Has<PolylineLayerSetting>();
        }
    }

    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Fixed,
        FixedRadius = AppPreference.StrokeWireframeRadius,
        AllowIntersection = false,
    };
    private StrokeView _dashPreview;

    public override void Prepare(CursorButtonData data)
    {
    }

    public override void Start(CursorButtonData data)
    {
        _dashPreview = new StrokeView();
        _dashPreview.Material = AutoloadRendering.DashWireframeMaterial;
        var layerE = SelectionManager.WorkingLayer.Value;
        var layerView = layerE.Get<PolylineLayerView>();
        layerView.AddChild(_dashPreview);

        _generator.Start(data);
    }

    public override void Interacting(CursorMotionData data)
    {
        _generator.Update(data);
        ImmutableArray<Vector2> points = [.._generator.Positions, _generator.Positions[0]];
        _dashPreview.SetGeometry(points, AppPreference.StrokeWireframeRadius);
    }

    public override void End(CursorButtonData data)
    {
        var layerE = SelectionManager.WorkingLayer.Value;
        var setting = new FilledPolygonSetting() { Color = { Value = tool.Color.Value } };
        var cmd = new NewFilledPolygonCmd(layerE, setting);
        var polygonE = cmd.InitEntity();
        var geom = new PolylineGeometry()
        {
            Positions = [.._generator.Positions],
        };
        cmd.Combine(new SetPolylineGeometryCmd(polygonE, geom)).Commit();
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