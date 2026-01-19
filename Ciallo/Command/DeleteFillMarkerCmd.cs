using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class DeleteFillMarkerCmd : CommandBase
{
    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    
    protected override void Do(Entity targetE)
    {
        throw new System.NotImplementedException();
    }
    
    protected override void Undo(Entity targetE)
    {
        throw new System.NotImplementedException();
    }
}