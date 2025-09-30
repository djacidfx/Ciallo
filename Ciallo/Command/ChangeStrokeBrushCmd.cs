using Arch.Core;

namespace Ciallo.Command;

public class ChangeStrokeBrushCmd(Entity StrokeE, Entity newBrushE) : CommandBase
{
    private Entity _newBrushE = newBrushE;
    private Entity _oldBrushE = Entity.Null;
    
    public override void Do()
    {
        
    }

    public override void Undo()
    {
        
    }
}