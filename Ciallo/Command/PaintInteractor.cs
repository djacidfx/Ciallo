using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Command;

public class PaintInteractor : InteractorBase
{
    public override bool CanInteract => WorkingLayer.Has<VectorLayerSetting>();

    private Vector2 _startPos;

    public override void Start(CursorButtonData data)
    {
        _startPos = data.WorldPosition;
        
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