
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Command;

public abstract class InteractorBase
{
    public abstract bool CanInteract { get; }
    
    public abstract void Activated();
    
    public abstract void Deactivated();

    public abstract void InteractionStart(CursorButtonData data);
    
    public abstract void Interacting(CursorMotionData data);
    
    public abstract void InteractionEnd(CursorButtonData data);
    
    public abstract void Cancel();
}