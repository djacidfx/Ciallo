using System;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiControl;

[GlobalClass, Tool]
public partial class LayerFolderContainer : FoldableVBoxContainer
{
    public int Level = -1;
    public bool IsRoot => Level == 0;
    /// <summary>
    /// If a layer has a cel folder ancestor, it is being celed.
    /// Celed folder prohibits adding another CellFolderLayer descendant and avoids nested CellFolderLayer
    /// </summary>
    public bool IsBeingCeled;

    public override void _EnterTree()
    {
        // Compute level
        if (GetParent() is not LayerFolderContainer parent)
        {
            Level = 0;
            IsBeingCeled = false;
        }
        else
        {
            var parentLevel = parent.Level;
            if (parentLevel == -1)
            {
                throw new Exception("Unexpected parent level");
            }
            Level = parentLevel + 1;
            IsBeingCeled = parent.IsBeingCeled;
        }
    }

    public override void _ExitTree()
    {
        Level = -1;
        IsBeingCeled = false;
    }

    public LayerFolderContainer ObserveIsExpanded(ReactiveProperty<bool> property, out IDisposable sub)
    {
        sub = property.Subscribe(v => IsExpanded = v);
        return this;
    }
}