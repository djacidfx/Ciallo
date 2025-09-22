using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.Command;

public class NewBrushCmd(BrushSetting inputSetting) : CommandBase
{
    public Entity BrushE = Entity.Null;
    private IDisposable _nameSubscription;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(BrushE);
    
    public override void Do()
    {
        if (BrushE == Entity.Null)
        {
            BrushE = WorkingWorld.Create();
            var s = inputSetting.Clone();
            s.Labels.Remove(BrushLabel.BuiltIn);
            BrushE.Add(s);
        }
        // Data
        BrushE.Add(new ToSerializeTag());
        var brushM = Document.Get<BrushManager>();
        brushM.Add(BrushE);
        
        // UI
        // Note: suppose to have a dedicate custom widget to handle this.
        var setting = BrushE.Get<BrushSetting>();
        var list = Document.Get<DocumentBrushList>();
        list.AddItem(setting.Name.Value);
        _nameSubscription = setting.Name.Subscribe(s =>
        {
            var idx = brushM.Brushes.IndexOf(BrushE);
            list.SetItemText(idx, s);
        });
    }

    public override void Undo()
    {
        // UI
        _nameSubscription.Dispose();
        _nameSubscription = null;
        var brushM = Document.Get<BrushManager>();
        var list = Document.Get<DocumentBrushList>();
        var idx = brushM.Brushes.IndexOf(BrushE);
        list.RemoveItem(idx);
        
        // Data
        brushM.Remove(BrushE);
        BrushE.Remove<ToSerializeTag>();
    }
}