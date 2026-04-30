using System;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiControl;

[GlobalClass, Tool]
public partial class LayerFolderContainer : FoldableVBoxContainer
{
    public int Level = -1;

    public override void _EnterTree()
    {
        // Compute level
        if (GetParent() is not LayerFolderContainer parent)
        {
            Level = 0;
        }
        else
        {
            var parentLevel = parent.Level;
            if (parentLevel == -1)
            {
                throw new Exception("Unexpected parent level");
            }
            Level = parentLevel + 1;
        }
    }

    public LayerFolderContainer ObserveIsExpanded(ReactiveProperty<bool> property, out IDisposable sub)
    {
        sub = property.Subscribe(v => IsExpanded = v);
        return this;
    }
}