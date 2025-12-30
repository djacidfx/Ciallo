using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// Interface for the classes that handle canvas interactions. Tools hold one or more of these interactive session.
/// Implement users' tools logics and behaviors.
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

    // Transition destination call this before the transition source calling End() or Cancel()
    public void BeforeSrcEnd(IInteractiveSession session) { }
    // after the transition source calling End() or Cancel()
    public void AfterSrcEnd(IInteractiveSession session) { }

    public void Start(CursorButtonData data);

    public void Interacting(CursorMotionData data);

    public void End(CursorButtonData data);

    // Transition source calls
    public void BeforeDstStart(IInteractiveSession session) { }
    public void AfterDstStart(IInteractiveSession session) { }

    public void Cancel();

    // Return true if the event is handled
    // For those "active interaction sessions", this function should always return true so all events should be marked as handled.
    // "Active interaction sessions" means those sessions triggered by user input other than simply hovering, e.g. dragging, key pressing.
    public bool OnKey(InputEventKey key, CursorButtonData data);
}