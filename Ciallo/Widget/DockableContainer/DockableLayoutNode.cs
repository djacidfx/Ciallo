using Godot;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class DockableLayoutNode : Resource
{
    // Runtime-only back-link; serializing it would make the resource tree cyclic.
    public DockableLayoutSplit Parent { get; set; }

    public virtual bool IsEmpty() => true;

    public virtual string[] GetNames() => [];

    protected void EmitTreeChanged()
    {
        // DockableLayout listens only to Root, so nested resource edits must reach every ancestor.
        DockableLayoutNode node = this;
        while (node != null)
        {
            node.EmitChanged();
            node = node.Parent;
        }
    }
}
