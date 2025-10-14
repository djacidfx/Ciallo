using Ciallo.Data;
using Ciallo.NodeControl;
using Massive;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return layerE.IsNotNull() && layerE.Has<ImageLayerSetting>();
        }
    }

    public override void Start(CursorButtonData data)
    {
        
    }
    

    public override void Cancel()
    {
    }
}