using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Command;

public abstract class InteractorBase
{
    public World WorkingWorld { get; set; } = WorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public Entity WorkingLayer => Document.Get<SelectionManager>().WorkingLayer;
    
    public abstract bool CanInteract { get; }

    public abstract void Start(CursorButtonData data);
    
    public abstract void Interacting(CursorMotionData data);
    
    public abstract void End(CursorButtonData data);
    
    public abstract void Cancel(CursorButtonData data);
}