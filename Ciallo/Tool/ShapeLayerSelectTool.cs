using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Select)]
public class ShapeLayerSelectTool : StateMachineToolBase
{
    public readonly PolylineSelectHover Hover = new();
    public readonly PolylineTransformInteractor Transform = new();
    public readonly RectSelectPolylineInteractor Select = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitDynamic(Press(MouseButton.Left), () =>
            {
                if (Hover.CanTransform)
                    return Transform;
                return Select;
            });

        Configure(Transform)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);

        Configure(Select)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        bool isShapeLayer = e.Has<ShapeLayerSetting>();
        bool isVectorFillLayer = e.Has<VectorFillLayerSetting>();
        return !e.IsDyingOrDead && (isShapeLayer || isVectorFillLayer);
    }

    public override void OnActivated()
    {
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    public override void OnDeactivated()
    {
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}