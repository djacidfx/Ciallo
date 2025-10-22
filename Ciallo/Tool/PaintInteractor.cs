using System.Diagnostics;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintInteractor : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer.Value;
            bool layerAvailable = !l.IsNull && l.Has<PolylineLayerSetting>();
            bool brushAvailable = !SelectionManager.WorkingBrush.Value.IsNull || AppBrushLibrary.HasSelection;

            return layerAvailable && brushAvailable;
        }
    }

    private Entity _brushE;
    private StrokeView _strokePreview;

    private Stopwatch _interactStopwatch;

    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Sampled,
    };

    public override void Prepare(CursorButtonData data)
    {
    }

    public override void Start(CursorButtonData data)
    {
        // Shen: I guess this will improve graphics responsiveness
        OS.LowProcessorUsageMode = false;
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _interactStopwatch = Stopwatch.StartNew();

        // Selection in brush library has higher priority
        if (AppBrushLibrary.HasSelection)
        {
            var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
            new NewBrushCmd(setting).Combine(new ChangeWorkingBrushCmd(^1)).Commit();
        }
        _brushE = SelectionManager.WorkingBrush.Value;
        var brushMaterial = _brushE.Get<BrushMaterial>();

        _strokePreview = new StrokeView();
        _strokePreview.Material = brushMaterial;
        var layerE = SelectionManager.WorkingLayer.Value;
        var layerView = layerE.Get<PolylineLayerView>();
        layerView.AddChild(_strokePreview);

        var brushSetting = _brushE.Get<BrushSetting>();
        _generator.RadiusSampler = PolylineInteractiveGenerator.BrushToRadiusSampler(brushSetting);

        _generator.Start(data);
    }

    public override void Interacting(CursorMotionData data)
    {
        long deltaMs = _interactStopwatch.ElapsedMilliseconds;
        // GD.Print($"[PaintInteractor] Interacting delta: {deltaMs} ms");
        _interactStopwatch.Restart();

        _generator.Update(data);
        _strokePreview.SetGeometry(_generator.Points, _generator.Radii);
    }

    public override void End(CursorButtonData data)
    {
        var layerE = SelectionManager.WorkingLayer.Value;
        var cmd = new NewStrokeCmd(layerE);
        var strokeE = cmd.InitEntity();
        var geom = new PolylineGeometry()
        {
            Points = [.._generator.Points],
            Radii = [.._generator.Radii],
        };
        cmd.Combine(new ChangeStrokeBrushCmd(strokeE, _brushE))
            .Combine(new SetPolylineGeometryCmd(strokeE, geom))
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
        _strokePreview.QueueFree();
        OS.LowProcessorUsageMode = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}