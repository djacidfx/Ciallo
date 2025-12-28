using Ciallo.Data;
using Ciallo.Geometry;
using Frent;

namespace Ciallo.Tool;

/// <summary>
/// Base class for the objects that handle canvas interactions. Tools hold one or more of these interactors.
/// Interactors actually implement users' tools logics and behaviors.
/// </summary>
/// <remarks>
/// Key design idea:
/// Separating interaction logic from how to trigger an interactive session, which allows us to support key remapping and more.
/// E.g. Stroke drag interactor should not know it's started by dragging left mouse button, or pressing the 'G' key(like Blender)
/// </remarks>
public abstract class InteractorBase
{
    public Entity Document => AppDocumentManager.WorkingDocument.Value;
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();

    // Instantly called after CanInteract, no state switching, return whether to start interaction
    public abstract bool Prepare(CursorButtonData data);
    // Called after some tool state switching
    public abstract void Start(CursorButtonData data);

    public abstract void Interacting(CursorMotionData data);

    public abstract void End(CursorButtonData data);

    public abstract void Cancel();
}