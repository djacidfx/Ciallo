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
        _preview = new GapBridgePreviewManager(Document.Get<WorldOverlay>(), Arrangement.SourceShapes);
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
        _arrReadySub.Dispose();
        _arrReadySub = null;
        _preview.Dispose();
        _preview = null;
        Arrangement = null;
    }

    public bool TryPickBridge(Vector2 worldPosition, out GapBridge bridge)
    {
        return _preview.TryPickBridge(worldPosition, out bridge);
    }

    private void OnClick()
    {
        if (Arrangement.ArrReady.CurrentValue == null)
            return;

        var clickPosition = LatestCursor.WorldPosition;
        if (!TryPickBridge(clickPosition, out var bridge) &&
            !Hover.TryGetHoveredBridge(out bridge))
            return;

        CommitBridge(bridge);
        if (Machine.State is GapBridgeHover hover)
            hover.RefreshHover(clickPosition);
    }

    private void CommitBridge(GapBridge bridge)
    {
        var sourceGeometry = bridge.SourceCurve.Get<SampledPolyline>();
        var repairedPositions = GapBridgeRepairGeometry.BuildRepairedPositions(Arrangement.ArrReady.CurrentValue, bridge);

        new CommandBuilder("Gap Bridge", bridge.SourceCurve)
            .SetPolylineGeometry(
                repairedPositions,
                sourceGeometry.Radii.Value,
                sourceGeometry.Pressures.Value,
                sourceGeometry.Tilts.Value)
            .Commit();
    }
}
