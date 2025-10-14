using Ciallo.NodeControl;

namespace Ciallo.Tool;

public abstract class HoverBase : InteractorBase
{
    // Optional to implement:
    public override void Start(CursorButtonData _)
    {
    }

    public override void Interacting(CursorMotionData _)
    {
    }

    // Don't implement
    public sealed override void End(CursorButtonData _)
    {
    }
    
    // Must implement cancel
}