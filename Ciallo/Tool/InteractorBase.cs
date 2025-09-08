using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// Base class for the objects that handle canvas interactions. Tools hold one or more of these interactors.
/// Interactors actually implement tools' logics and behaviors.
/// </summary>
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