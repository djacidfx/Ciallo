using Ciallo.Data;
using Ciallo.Geometry;
using Frent;

namespace Ciallo.Tool;

/// <summary>
/// Hover does not modify document, only provide visual feedback.
/// Once the document is about to be modified (by interactor, by command undo, redo) hover should call end().   
/// </summary>
public abstract class HoverBase
{
    public Entity Document => AppDocumentManager.WorkingDocument.Value;
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();

    public abstract void Start();

    public virtual void Interacting(CursorMotionData _)
    {
    }

    // Must implement cancel
    // Called after ESC pressed, switch tool.
    public abstract void End();
}