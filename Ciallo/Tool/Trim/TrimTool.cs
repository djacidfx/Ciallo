using System;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Trim)]
public class TrimTool : ToolBase
{
    public readonly TrimHover Hover = new();
    public readonly TrimInteractor Trim = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitIf(Press(MouseButton.Left), Trim, () => true);

        Configure(Trim)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs) =>
        layerEs.Length == 1 && layerEs.Single().Has<ShapeLayerSetting>();

    public override void OnActivated()
    {

    }

    public override void OnDeactivated()
    {

    }
}
