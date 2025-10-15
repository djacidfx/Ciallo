using System;
using Ciallo.Data;
using Ciallo.NodeControl;
using Massive;

namespace Ciallo.Tool;

public class PaintFillInteractor : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer.Value;
            return l.IsNotNull() && l.Has<PolylineLayerSetting>();
        }
    }

    public override void Start(CursorButtonData data)
    {
    }

    public override void Interacting(CursorMotionData data)
    {
        throw new NotImplementedException();
    }

    public override void End(CursorButtonData data)
    {
        throw new NotImplementedException();
    }

    public override void Cancel()
    {
        throw new NotImplementedException();
    }
}