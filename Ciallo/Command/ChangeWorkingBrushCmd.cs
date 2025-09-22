using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

public class ChangeWorkingBrushCmd(int idx) : CommandBase
{
    public Entity OldBrushE = Entity.Null;
    public Entity NewBrushE = Entity.Null;
    
    public override void Do()
    {
        var bm = Document.Get<BrushManager>();
        var sm = Document.Get<SelectionManager>();
        
        // data
        OldBrushE = sm.SelectedBrush.Value;
        NewBrushE = bm.Brushes[idx];
        sm.SelectedBrush.Value = NewBrushE;
        
        // UI
        var brushList = Document.Get<DocumentBrushList>();
        brushList.Select(idx);
    }

    public override void Undo()
    {
        var bm = Document.Get<BrushManager>();
        var sm = Document.Get<SelectionManager>();
        
        // UI
        var brushList = Document.Get<DocumentBrushList>();
        if(OldBrushE == Entity.Null) brushList.DeselectAll();
        else
        {
            var oldIdx = bm.Brushes.IndexOf(OldBrushE);
            if(oldIdx == -1) throw new InvalidOperationException("Old brush not found in brush manager.");
            brushList.Select(oldIdx);
        }
        
        // data
        sm.SelectedBrush.Value = OldBrushE;
    }
}