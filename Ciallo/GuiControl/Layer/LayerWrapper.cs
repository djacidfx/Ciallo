using System;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Wraps every layer's <see cref="LayerBlock"/> (as the <see cref="FoldableVBoxContainer.Title"/>)
/// and holds its child layers as content nodes.
/// One instance per layer entity; hierarchy mirrors <see cref="Data.LayerTreeNode"/> hierarchy.
/// </summary>
[GlobalClass, Tool]
public partial class LayerWrapper : FoldableVBoxContainer
{
    public int Level = -1;
    public bool IsRoot => Level == 0;

    /// <summary>
    /// True when this layer lives inside a CelFolder subtree (has a CelFolder ancestor).
    /// Propagated transitively on <see cref="_EnterTree"/> via the parent wrapper's Title block.
    /// </summary>
    public bool IsBeingCeled;

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
            // parent.Title is the LayerBlock of the parent layer.
            IsBeingCeled = parent.IsBeingCeled || (parent.Title is LayerBlock lb && lb.IsCelFolder);
        }
    }

    public override void _ExitTree()
    {
        Level = -1;
        IsBeingCeled = false;
    }

    public LayerWrapper ObserveIsExpanded(ReactiveProperty<bool> property, out IDisposable sub)
    {
        sub = property.Subscribe(v => IsExpanded = v);
        return this;
    }
}

