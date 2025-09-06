using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;

namespace Ciallo.Command;

public class PaintInteractor : InteractorBase
{
    public override bool CanInteract => Selection.WorkingLayer.Has<VectorLayerSetting>();

    public override void Start(CursorButtonData data)
    {
        throw new System.NotImplementedException();
    }

    public override void Interacting(CursorMotionData data)
    {
        throw new System.NotImplementedException();
    }

    public override void End(CursorButtonData data)
    {
        throw new System.NotImplementedException();
    }

    public override void Cancel(CursorButtonData data)
    {
        throw new System.NotImplementedException();
    }
}