using System;
using Ciallo.Data;
using Ciallo.Geometry;

namespace Ciallo.Tool;

public class PaintFillInteractor : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer.Value;
            return !l.IsNull && l.Has<PolylineLayerSetting>();
        }
    }

    public override void Prepare(CursorButtonData data)
    {
    }

    public override void Start(CursorButtonData data)
    {
        throw new NotImplementedException();
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