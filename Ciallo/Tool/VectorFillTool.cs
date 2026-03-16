using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.VectorFill)]
public class VectorFillTool : StateMachineToolBase
{
    public readonly VectorFillHover Hover = new();
    public readonly PaintVectorFillMarkerInteractor Left = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitIf(Press(MouseButton.Left), Left,
                () => WorkingLayer.Has<VectorFillLayerSetting>());
        Configure(Left)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        bool isVectorFillLayer = e.Has<VectorFillLayerSetting>();
        bool isShapeLayer = e.Has<ShapeLayerSetting>();
        return !e.IsDyingOrDead && (isVectorFillLayer || isShapeLayer);
    }

    public override void OnActivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = true;
    }

    public override void OnDeactivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = false;
    }
}