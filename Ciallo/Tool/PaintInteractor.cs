using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintInteractor : InteractiveSessionBase
{
    private Entity _brushE;
    private StrokeView _strokePreview;

    private readonly PolylineInteractiveGenerator _generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Sampled,
    };

    public override void Start(CursorButtonData data)
    {
        OS.LowProcessorUsageMode = false; // Reduce input lag if user has device in high reporting rate.
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        // Selection in brush library has higher priority
        if (AppBrushLibrary.HasSelection)
        {
            var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
            new CommandBuilder(Document.World.Create())
                .NewBrush(setting).SetWorkingBrush().Commit();
            AppBrushLibrary.SelectedIndex.Value = -1;
        }
        _brushE = Document.Get<SelectionManager>().WorkingBrush.Value;

        var brushMaterial = _brushE.Get<BrushMaterial>();

        _strokePreview = new StrokeView();
        _strokePreview.Material = brushMaterial;
        var layerView = WorkingLayer.Get<ShapeLayerView>();
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

        var geom = new PolylineGeometry()
        {
            Positions = [.._generator.Positions],
            Radii = [.._generator.Radii],
            Pressures = [.._generator.Pressures],
            Tilts = [.._generator.Tilts],
        };
        new CommandBuilder(WorkingLayer.World.Create())
            .NewStroke()
            .AddToLayerTree(WorkingLayer)
            .SetProperty(e => e.Get<StrokeSetting>().BrushE, _brushE)
            .SetPolylineGeometry(geom)
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
        _strokePreview.QueueFree();
        OS.LowProcessorUsageMode = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}