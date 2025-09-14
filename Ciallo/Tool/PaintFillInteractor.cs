using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;

namespace Ciallo.Tool;

public class PaintFillInteractor : InteractorBase
{
    public override bool CanInteract 
    {
        get
        {
            var l = SelectionManager.WorkingLayer;
            return l != Entity.Null && l.Has<PolylineLayerSetting>();
        }
    }

    public override void Start(CursorButtonData data)
    {
        
    }
}