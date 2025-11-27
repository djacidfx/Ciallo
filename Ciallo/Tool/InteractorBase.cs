using Ciallo.Data;
using Ciallo.Geometry;
using Frent;

namespace Ciallo.Tool;

/// <summary>
/// Base class for the objects that handle canvas interactions. Tools hold one or more of these interactors.
/// Interactors actually implement users' tools logics and behaviors.
/// In order to support key remapping, interactors shouldn't know "how to start himself".
/// E.g. Stroke drag interactor doesn't know it's started by dragging left mouse button, or pressing the 'G' key(like Blender)
/// </summary>
public abstract class InteractorBase
{
    public World WorkingWorld => AppWorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();

    // Instantly called after CanInteract, no state switching, return whether to start interaction
    public abstract bool Prepare(CursorButtonData data);
    // Called after some tool state switching
    public abstract void Start(CursorButtonData data);

    public abstract void Interacting(CursorMotionData data);

    public abstract void End(CursorButtonData data);

    public abstract void Cancel();
}