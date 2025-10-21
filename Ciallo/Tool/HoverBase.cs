using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Tool;

public abstract class HoverBase
{
    public World WorkingWorld => AppWorldManager.WorkingWorld.Value;
    public Entity Document => WorkingWorld.Document();
    public SelectionManager SelectionManager => Document.Get<SelectionManager>();

    public abstract void Start();

    public virtual void Interacting(CursorMotionData _)
    {
    }

    // Must implement cancel
    // Called after ESC pressed, switch tool, switch layer.
    public abstract void End();
}