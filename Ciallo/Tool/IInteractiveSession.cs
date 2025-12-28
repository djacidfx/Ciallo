using Ciallo.Data;
using Ciallo.Geometry;
using Frent;

namespace Ciallo.Tool;

/// <summary>
/// Interface for the classes that handle canvas interactions. Tools hold one or more of these interactive session.
/// Interactors actually implement users' tools logics and behaviors.
/// </summary>
/// <remarks>
/// Key design idea:
/// Separating interactive logic from how to trigger an interactive session。This allows us to support key remapping and more.
/// E.g. Stroke drag interactor should not know it's started by dragging left mouse button, or pressing the 'G' key(like Blender)
/// The tool script implement ITool is responsible for triggering the interactive session according to user input and tool state.
/// </remarks>
public interface IInteractiveSession
{
    public Entity Document => AppDocumentManager.WorkingDocument.Value;
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();

    public void Start(CursorButtonData data);

    public void Interacting(CursorMotionData data);

    public void End(CursorButtonData data);

    public void Cancel();
}