using Ciallo.NodeControl;

namespace Ciallo.Tool;

public abstract class HoverBase : InteractorBase
{
    // Optional to implement
    public virtual void Start(CursorMotionData data)
    {
    }

    public override void Interacting(CursorMotionData _)
    {
    }

    // Don't implement
    public sealed override void End(CursorButtonData _)
    {
    }
    
    public sealed override void Start(CursorButtonData _)
    {
    }
    
    // Must implement cancel
}