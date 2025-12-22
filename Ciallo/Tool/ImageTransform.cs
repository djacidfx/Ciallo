using System.Linq;
using Ciallo.Data;
using Ciallo.Widget;
using Frent;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Select)]
public class ImageTransform : CommonToolBase
{
    public ImageTransform()
    {
        var hover = new ImageTransformHover();
        HoverInteractor = hover;
        LeftInteractor = new ImageTransformInteractor(hover);
    }

    public override void DrawProperty(PropertyContainer container)
    {
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        return layerEs.Length == 1 && layerEs.Single().Has<ImageLayerSetting>();
    }
}