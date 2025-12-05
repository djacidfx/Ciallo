using Ciallo.Data;
using Ciallo.GuiBinding;
using Ciallo.Tool;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

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

    public override bool OnSwitchLayer(Entity newLayerE)
    {
        if (newLayerE.IsDeletedOrNull()) return false;
        return newLayerE.Has<PolylineLayerSetting>();
    }
}