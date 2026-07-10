using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool, GlobalClass]
public partial class DockableLayoutNode : Resource
{
    public DockableLayoutSplit Parent { get; set; }

    public virtual bool IsEmpty() => true;

    public virtual string[] GetNames() => [];

    protected void EmitTreeChanged()
    {
        DockableLayoutNode node = this;
        while (node != null)
        {
            node.EmitChanged();
            node = node.Parent;
        }
    }
}
