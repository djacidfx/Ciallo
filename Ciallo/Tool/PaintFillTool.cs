using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.PaintFill)]
public partial class PaintFillTool : StateMachineToolBase
{
    public readonly ReactiveProperty<Color> Color = new(Colors.Black);

    public readonly PaintFillHover Hover = new();
    public readonly PaintFillInteractor Left;

    public PaintFillTool()
    {
        Left = new(this);
    }

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .Permit(Press(MouseButton.Left), Left);

        Configure(Left)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddProperty("Fill Color", new ColorPickerButton
        {
            CustomMinimumSize = new(0, 32),
        }.BindColor(Color));
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        return !e.IsDyingOrDead && e.Has<ShapeLayerSetting>();
    }
}