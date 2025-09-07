using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

public abstract class InteractorBase
{
    public World WorkingWorld => WorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();
    
    public abstract bool CanInteract { get; }

    public abstract void Start(CursorButtonData data);
    
    public abstract void Interacting(CursorMotionData data);
    
    public abstract void End(CursorButtonData data);
    
    public abstract void Cancel();
}