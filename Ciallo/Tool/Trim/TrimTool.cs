using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

/// <summary>
/// Trim tool aims to be "visually/feelingly topologically robust"
/// Not truly topologically correct, as long as users feel it correct.
/// Ciallo is a drawing app, not a CAD topology editor: prefer visually
/// correct 95% behavior over preserving every tiny real stroke or junction.
/// </summary>
[RegisterTool(ToolButton.Trim)]
public class TrimTool : ToolBase
{
    public readonly TrimHover Hover = new();
    public readonly TrimInteractor Trim = new();

    // On shape layers this is tool-owned; on vector fill layers it is the layer-owned manager.
    public ArrangementManager Arrangement { get; private set; }

    private IDisposable _arrReadySub;
    private bool _ownsArrangement;

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitIf(Press(MouseButton.Left), Trim, () => Arrangement?.ArrReady.CurrentValue != null);

        Configure(Trim)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var layerE = layerEs[0];
        return layerE.Has<ShapeLayerSetting>() || layerE.Has<VectorFillLayerSetting>();
    }

    public override void OnActivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
        {
            Arrangement = WorkingLayer.Get<ArrangementManager>();
            _ownsArrangement = false;
        }
        else
        {
            Arrangement = new ArrangementManager();
            _ownsArrangement = true;
            Arrangement.Observe(WorkingLayer.Get<ShapeLayerPolylineIndex>());
            Arrangement.SyncModification();
        }

        // Refresh hover cursor when the snapshot becomes ready / not-ready.
        _arrReadySub = Arrangement.ArrReady.Subscribe(_ =>
        {
            if (Machine.State is TrimHover hover)
                hover.RefreshCursor();
        });
    }

    public override void OnDeactivated()
    {
        _arrReadySub?.Dispose();
        _arrReadySub = null;
        if (_ownsArrangement)
        {
            Arrangement.DesyncModification();
            Arrangement.Dispose();
        }
        Arrangement = null;
        _ownsArrangement = false;
    }
}
