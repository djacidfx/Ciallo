namespace Ciallo.Command;

// Used for combining multiple commands and naming them.
public class EmptyCommand : CommandBase
{
    public override void Do() { }
    public override void Undo() { }
}