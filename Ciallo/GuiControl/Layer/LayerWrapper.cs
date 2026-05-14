using Ciallo.Widget;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Wraps every layer's <see cref="LayerBlock"/> (as the <see cref="FoldableVBoxContainer.Title"/>)
/// One instance per layer entity; hierarchy mirrors <see cref="Data.LayerTreeNode"/> hierarchy.
/// </summary>
[Tool]
public partial class LayerWrapper : FoldableVBoxContainer
{
    public int Level = -1;
    public bool IsRoot => Level == 0;

    /// <summary>
    /// True when this layer lives inside a CelFolder subtree (has a CelFolder ancestor).
    /// Propagated transitively on <see cref="_EnterTree"/> via the parent wrapper's Title block.
    /// </summary>
    public bool IsBeingCeled;
    public virtual LayerBlock Block => Title as LayerBlock;

    public LayerWrapper()
    {
        ReverseOrder = true;
    }

    public override void _EnterTree()
    {
        if (GetParent() is not LayerWrapper parent)
        {
            Level = 0;
            IsBeingCeled = false;
        }
        else
        {
            Level = parent.Level + 1;
            // Propagate transitively: celed if any ancestor is a CelFolder.
            IsBeingCeled = parent.IsBeingCeled || parent.Block?.IsCelFolder == true;
        }
    }

    public override void _ExitTree()
    {
        Level = -1;
        IsBeingCeled = false;
    }

    /// <summary>
    /// Returns true if this layer or any layer in its Godot-node subtree is a CelFolder.
    /// Walks the <see cref="LayerWrapper"/> children directly — no entity component lookups.
    /// </summary>
    public bool HasCelFolderInSubtree()
    {
        if (Block?.IsCelFolder == true) return true;
        foreach (Node child in GetChildren())
            if (child is LayerWrapper w && w.HasCelFolderInSubtree())
                return true;
        return false;
    }
}