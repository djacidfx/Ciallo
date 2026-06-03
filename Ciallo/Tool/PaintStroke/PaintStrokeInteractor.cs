using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PaintStrokeInteractor : InteractiveSessionBase
{
    public Entity BrushE;
    public StrokeView StrokePreview;
    public bool IsTaperEnding = false;
    public readonly PolylineInteractiveGenerator Generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Sampled,
    };

    public static readonly ToolBase.Trigger PaintEnd = new("PaintEnd");

    public PaintStrokeInteractor()
    {
        MovingMinInterval = TimeSpan.Zero;
    }

    public override void Start(CursorButtonData data)
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        // Selection in brush library has higher priority
        if (AppStrokeBrushLibrary.HasSelection)
        {
            var setting = AppStrokeBrushLibrary.SelectedBrushSetting.CurrentValue;
            new CommandBuilder(Document.World.Create())
                .NewStrokeBrush(setting).SetWorkingStrokeBrush().Commit();
            AppStrokeBrushLibrary.SelectedIndex.Value = -1;
        }
        BrushE = Document.Get<SelectionManager>().WorkingStrokeBrush.Value;

        var brushMaterial = BrushE.Get<StrokeBrushMaterial>();

        StrokePreview = new StrokeView();
        StrokePreview.Material = brushMaterial;
        var layerView = WorkingLayer.Get<ShapeLayerView>();
        layerView.AddChild(StrokePreview);

        var brushSetting = BrushE.Get<StrokeBrushSetting>();
        Generator.RadiusSampler = brushSetting.ToRadiusSampler();

        Generator.Start(data);
        StrokePreview.SetGeometry(Generator.Positions, Generator.Radii, Generator.Pressures);
    }

    public override void Moving(CursorMotionData data)
    {
        Generator.Update(data);
        StrokePreview.SetGeometry(Generator.Positions, Generator.Radii, Generator.Pressures);
    }

    public override void OnMouseButton(InputEventMouseButton button, CursorButtonData data)
    {
        if (button.ButtonIndex == MouseButton.Left && button.IsReleased())
        {
            OnEndPaintButton();
        }
    }

    public void OnEndPaintButton()
    {
        if (IsTaperEnding) return;
        if (AppPreference.TaperDuration.Value <= TimeSpan.FromMilliseconds(1))
        {
            Tool.Machine.Fire(PaintEnd);
            return;
        }

        // ObserveOn(BeforeProcess) ensures the callback fires before _Process (where Timer ticks),
        // so the state machine transition happens before the next render step — no flickering.
        Observable.Timer(AppPreference.TaperDuration.Value)
            .ObserveOn(GodotFrameProvider.BeforeProcess)
            .Subscribe(_ =>
            {
                if (Tool.Machine.CanFire(PaintEnd))
                    Tool.Machine.Fire(PaintEnd);
            });

        IsTaperEnding = true;
        Generator.StartTaperEnding();
    }

    public override void End(CursorButtonData data)
    {
        Generator.End(data);

        new CommandBuilder(WorkingLayer.World.Create())
            .NewStroke()
            .AddToLayerTree(WorkingLayer)
            .SetProperty(e => e.Get<StrokeSetting>().BrushE, BrushE)
            .SetPolylineGeometry([..Generator.Positions], [..Generator.Radii], [..Generator.Pressures], [..Generator.Tilts])
            .Commit();
        Clear();
    }

    public override void Cancel() => Clear();
    public override bool OnKey(InputEventKey key, CursorButtonData data)
    {
        if (AppActions.ConfirmInteraction.IsPressedBy(key))
        {
            OnEndPaintButton();
        }
        return true;
    }

    public void Clear()
    {
        Generator.Clear();
        StrokePreview.QueueFree();
        StrokePreview = null;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        IsTaperEnding = false;
    }
}
