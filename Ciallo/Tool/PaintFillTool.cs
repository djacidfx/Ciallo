using Ciallo.Data;
using Ciallo.Tool;
using Ciallo.Widget;
using Frent;

public partial class PaintFillTool : CommonToolBase
{
    public PaintFillTool()
    {
        HoverInteractor = new PaintFillHover();
        LeftInteractor = new PaintFillInteractor();
    }

    public override void DrawProperty(PropertyContainer container)
    {
    }

    public override bool OnSwitchLayer(Entity newLayerE)
    {
        if (newLayerE.IsNull) return false;
        return newLayerE.Has<PolylineLayerSetting>();
    }
}