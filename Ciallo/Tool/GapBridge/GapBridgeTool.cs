using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;
using Stateless;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.GapBridge)]
public class GapBridgeTool : ToolBase
{
    public readonly GapBridgeHover Hover = new();

    public ArrangementManager Arrangement { get; private set; }

    private GapBridgePreviewManager _preview;
    private IDisposable _arrReadySub;

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .InternalTransition(Press(MouseButton.Left), OnClick);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var layerE = layerEs[0];
        return !layerE.IsDyingOrDead
            && (layerE.Has<ShapeLayerSetting>() || layerE.Has<VectorFillLayerSetting>());
    }

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddProperty("Max gap length",
            new SpinSlider
            {
                MinValue = 1f,
                MaxValue = 128,
                Step = 1f,
                ExpEdit = true,
                AllowGreater = true,
            }.BindNumber(AppPreference.GapBridgeDetectMaxGapLength));

        base.DrawProperty(container);
    }

    public override void OnActivated()
    {
        Arrangement = WorkingLayer.Get<ArrangementManager>();
        _preview = new GapBridgePreviewManager(Document.Get<WorldOverlay>());
        _preview.Refresh(Arrangement.ArrReady.CurrentValue);

        _arrReadySub = Arrangement.ArrReady.Subscribe(arr =>
        {
            _preview.Refresh(arr);
            if (Machine.State is GapBridgeHover hover)
                hover.RefreshCursor();
        });
    }

    public override void OnDeactivated()
    {
        _arrReadySub?.Dispose();
        _arrReadySub = null;
        _preview?.Dispose();
        _preview = null;
        Arrangement = null;
    }

    public bool TryPickTarget(Vector2 worldPosition, out GapBridgeTarget target)
    {
        target = default;
        return _preview?.TryPickTarget(worldPosition, out target) == true;
    }

    private void OnClick()
    {
        if (Arrangement?.ArrReady.CurrentValue == null)
            return;

        var clickPosition = LatestCursor.WorldPosition;
        if (!TryPickTarget(clickPosition, out var target) &&
            !Hover.TryGetHoveredTarget(out target))
            return;

        CommitBridge(target);
        if (Machine.State is GapBridgeHover hover)
            hover.RefreshHover(clickPosition);
    }

    private void CommitBridge(GapBridgeTarget target)
    {
        var candidate = target.Candidate;
        if (!GapBridgeGeometry.TryResolveCandidate(candidate, out var fromPoint, out var toPoint))
            return;

        var bridgePolyline = GapBridgeRepairGeometry.BuildPolyline(candidate, fromPoint, toPoint);
        if (bridgePolyline.Length < 2)
            return;

        target = new GapBridgeTarget(candidate, fromPoint, toPoint, bridgePolyline);
        var targetLayer = ResolveTargetLayer(target);
        if (targetLayer.IsNull || !targetLayer.IsAlive || !targetLayer.Has<ShapeLayerSetting>())
            return;

        var bridgeGeometry = GapBridgeRepairGeometry.BuildStrokeGeometry(target);
        if (bridgeGeometry.Positions.Length < 2)
            return;

        var bridgeE = WorkingLayer.World.Create();
        var cmd = new CommandBuilder("Gap Bridge", bridgeE);
        var styleSource = ResolveStyleSource(target);
        cmd = styleSource.IsNull ? cmd.NewStroke() : cmd.NewStroke(styleSource);
        cmd.AddToLayerTree(targetLayer)
            .SetPolylineGeometry(
                bridgeGeometry.Positions,
                bridgeGeometry.Radii,
                bridgeGeometry.Pressures,
                bridgeGeometry.Tilts)
            .Commit();
    }

    private Entity ResolveTargetLayer(GapBridgeTarget target)
    {
        if (WorkingLayer.Has<ShapeLayerSetting>())
            return WorkingLayer;

        if (TryGetShapeLayer(target.Candidate.FromCurve, out var fromLayer))
            return fromLayer;
        if (TryGetShapeLayer(target.Candidate.ToCurve, out var toLayer))
            return toLayer;

        if (WorkingLayer.Has<VectorFillLayerSetting>())
        {
            foreach (var layer in WorkingLayer.Get<VectorFillLayerSetting>().ReferenceLayers)
            {
                if (layer.IsAlive && layer.Has<ShapeLayerSetting>())
                    return layer;
            }
        }

        return Entity.Null;
    }

    private static Entity ResolveStyleSource(GapBridgeTarget target)
    {
        if (target.Candidate.FromCurve.IsAlive && target.Candidate.FromCurve.Has<StrokeSetting>())
            return target.Candidate.FromCurve;
        if (target.Candidate.ToCurve.IsAlive && target.Candidate.ToCurve.Has<StrokeSetting>())
            return target.Candidate.ToCurve;
        return Entity.Null;
    }

    private static bool TryGetShapeLayer(Entity shape, out Entity layer)
    {
        layer = Entity.Null;
        if (!shape.IsAlive || !shape.Has<LayerTreeNode>())
            return false;

        layer = shape.Get<LayerTreeNode>().ParentValue;
        return !layer.IsNull && layer.IsAlive && layer.Has<ShapeLayerSetting>();
    }
}
