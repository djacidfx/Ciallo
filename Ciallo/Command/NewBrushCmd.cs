using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

public class NewBrushCmd(BrushSetting inputSetting) : CommandBase
{
    public Entity BrushE = Entity.Null;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(BrushE);
    
    public override void Do()
    {
        if (BrushE == Entity.Null)
        {
            BrushE = WorkingWorld.Create();
            var setting = inputSetting.Clone();
            setting.Labels.Remove(BrushLabel.BuiltIn);
            BrushE.Add(setting, new ToSerializeTag());
        }
    }

    public override void Undo()
    {
        
    }
}