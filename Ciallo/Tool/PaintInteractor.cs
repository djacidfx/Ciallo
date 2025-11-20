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

    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Sampled,
    };

    public override void Prepare(CursorButtonData data)
    {
    }

    public override void Start(CursorButtonData data)
    {
        OS.LowProcessorUsageMode = false; // Reduce input lag if user has device in high reporting rate.
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        // Selection in brush library has higher priority
        if (AppBrushLibrary.HasSelection)
        {
            var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
            var cmd = new NewBrushCmd(setting);
            var brushE = cmd.InitEntity();
            cmd.Combine(new SetWorkingBrushCmd(brushE)).Commit();
        }
        _brushE = SelectionManager.WorkingBrush.Value;
        var brushMaterial = _brushE.Get<BrushMaterial>();

        _strokePreview = new StrokeView();
        _strokePreview.Material = brushMaterial;
        var layerE = SelectionManager.WorkingLayer.Value;
        var layerView = layerE.Get<PolylineLayerView>();
        layerView.AddChild(_strokePreview);

        var brushSetting = _brushE.Get<BrushSetting>();
        _generator.RadiusSampler = brushSetting.ToRadiusSampler();

        _generator.Start(data);
    }

    public override void Interacting(CursorMotionData data)
    {
        _generator.Update(data);
        _strokePreview.SetGeometry(_generator.Positions, _generator.Radii, _generator.Pressures);
    }

    public override void End(CursorButtonData data)
    {
        _generator.End(data);

        var layerE = SelectionManager.WorkingLayer.Value;
        var cmd = new NewStrokeCmd(layerE);
        var strokeE = cmd.InitEntity();
        var geom = new PolylineGeometry()
        {
            Positions = [.._generator.Positions],
            Radii = [.._generator.Radii],
            Pressures = [.._generator.Pressures],
            Tilts = [.._generator.Tilts],
        };
        cmd.Combine(new SetStrokeBrushCmd(strokeE, _brushE))
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