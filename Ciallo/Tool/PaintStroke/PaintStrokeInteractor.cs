using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintStrokeInteractor : InteractiveSessionBase
{
    public Entity BrushE;
    public StrokeView StrokePreview;
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
        var geometry = Generator.CurrentGeometry;
        StrokePreview.SetGeometry(geometry.Positions, geometry.Radii, geometry.Pressures);
    }

    public override void Moving(CursorMotionData data)
    {
        Generator.Update(data);
        var geometry = Generator.CurrentGeometry;
        StrokePreview.SetGeometry(geometry.Positions, geometry.Radii, geometry.Pressures);
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
        Tool.Machine.Fire(PaintEnd);
    }

    public override void End(CursorButtonData data)
    {
        Generator.End(data);
        var geometry = Generator.CurrentGeometry;

        new CommandBuilder(WorkingLayer.World.Create())
            .NewStroke()
            .AddToLayerTree(WorkingLayer)
            .SetProperty(e => e.Get<StrokeSetting>().BrushE, BrushE)
            .SetPolylineGeometry([..geometry.Positions], [..geometry.Radii], [..geometry.Pressures], [..geometry.Tilts])
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
    }
}
