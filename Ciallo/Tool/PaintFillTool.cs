using System.Linq;
using Ciallo.Data;
using Ciallo.GuiBinding;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.PaintFill)]
public partial class PaintFillTool : CommonToolBase
{
    public readonly ReactiveProperty<Color> Color = new(Colors.Black);

    public PaintFillTool()
    {
        HoverInteractor = new PaintFillHover();
        LeftInteractor = new PaintFillInteractor(this);
    }

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddProperty("Fill Color", new ColorPickerButton
        {
            CustomMinimumSize = new(0, 30),
        }.BindColor(Color));
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        return !e.IsDeletedOrNull() && e.Has<PolylineLayerSetting>();
    }
}