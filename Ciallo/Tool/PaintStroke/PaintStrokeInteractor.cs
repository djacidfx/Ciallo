using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PaintStrokeInteractor : InteractiveSessionBase
{
    public new PaintStrokeTool Tool => (PaintStrokeTool)base.Tool;
    public Entity BrushE;
    public StrokeView StrokePreview;
    public readonly PolylineInteractiveGenerator Generator = new()
    {
        Mode = PolylineInteractiveGenerator.RadiusMode.Sampled,
    };
    private PaintStrokeSnapTarget? _startSnapTarget;
    private bool? _startSnapAllowed;
    private PaintStrokeSnapTarget? _endSnapTarget;
    private readonly List<Vector2> _previewPositions = new(2048);
    private readonly List<float> _previewRadii = new(2048);
    private readonly List<float> _previewPressures = new(2048);
    private readonly List<Vector2> _previewTilts = new(2048);

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

        StrokePreview = new StrokeView
        {
            Material = brushMaterial
        };
        var layerView = WorkingLayer.Get<ShapeLayerView>();
        layerView.AddChild(StrokePreview);

        var brushSetting = BrushE.Get<StrokeBrushSetting>();
        Generator.RadiusSampler = brushSetting.ToRadiusSampler();

        _startSnapTarget = Tool.TryFindSnapTarget(data.WorldPosition, out var startTarget)
            ? startTarget
            : null;
        _startSnapAllowed = _startSnapTarget.HasValue ? null : false;
        _endSnapTarget = null;
        Generator.Start(data);
        UpdatePreview(data.WorldPosition);
    }

    public override void Moving(CursorMotionData data)
    {
        Generator.Update(data);
        UpdatePreview(data.WorldPosition);
    }

    public override void End(CursorButtonData data)
    {
        var geometry = BuildCommitGeometry(data);

        new CommandBuilder(WorkingLayer.World.Create())
            .NewStroke()
            .AddToLayerTree(WorkingLayer)
            .SetProperty(e => e.Get<StrokeSetting>().BrushE, BrushE)
            .SetPolylineGeometry(geometry.Positions, geometry.Radii, geometry.Pressures, geometry.Tilts)
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

    public void Clear()
    {
        Generator.Clear();
        StrokePreview.QueueFree();
        StrokePreview = null;
        _startSnapTarget = null;
        _startSnapAllowed = null;
        _endSnapTarget = null;
        Tool.SnapPreview.Hide();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    protected PaintStrokeGeometry BuildCommitGeometry(CursorButtonData data)
    {
        Generator.End(data);
        RefreshStartSnapAllowed(Generator.CurrentGeometry);
        RefreshEndSnapTarget(data.WorldPosition);
        return PaintStrokeSnap.BuildGeometry(
            Generator.CurrentGeometry,
            _startSnapAllowed == true ? _startSnapTarget : null,
            EndSnapTargetIfAllowed(Generator.CurrentGeometry));
    }

    private void UpdatePreview(Vector2 worldPosition)
    {
        var generatorGeometry = Generator.CurrentGeometry;
        RefreshStartSnapAllowed(generatorGeometry);
        RefreshEndSnapTarget(worldPosition);
        var endSnapTarget = EndSnapTargetIfAllowed(generatorGeometry);
        PaintStrokeSnap.FillGeometry(
            generatorGeometry,
            _startSnapAllowed == true ? _startSnapTarget : null,
            endSnapTarget,
            _previewPositions,
            _previewRadii,
            _previewPressures,
            _previewTilts);
        StrokePreview.SetGeometry(_previewPositions, _previewRadii, _previewPressures);
        UpdateSnapPreview(generatorGeometry, worldPosition, endSnapTarget);
    }

    private void RefreshStartSnapAllowed(PolylineGeneratorGeometry geometry)
    {
        if (_startSnapAllowed.HasValue || _startSnapTarget is not { } startTarget)
            return;

        if (PaintStrokeSnap.TryResolveStartDirection(geometry.Positions, startTarget, out var allowed))
            _startSnapAllowed = allowed;
    }

    private void RefreshEndSnapTarget(Vector2 worldPosition)
    {
        _endSnapTarget = Tool.TryFindSnapTarget(worldPosition, out var endTarget)
            ? endTarget
            : null;
    }

    private void UpdateSnapPreview(
        PolylineGeneratorGeometry geometry,
        Vector2 worldPosition,
        PaintStrokeSnapTarget? endSnapTarget)
    {
        if (endSnapTarget is { } end)
        {
            Tool.SnapPreview.Show(worldPosition, end.HitPoint);
            return;
        }

        if (_startSnapAllowed == true && _startSnapTarget is { } startTarget)
        {
            Tool.SnapPreview.Show(startTarget.HitPoint, geometry.Positions[0]);
            return;
        }

        Tool.SnapPreview.Hide();
    }

    private PaintStrokeSnapTarget? EndSnapTargetIfAllowed(PolylineGeneratorGeometry geometry)
    {
        return _endSnapTarget is { } endTarget && PaintStrokeSnap.EndDirectionAllowsSnap(geometry.Positions, endTarget)
            ? endTarget
            : null;
    }
}
