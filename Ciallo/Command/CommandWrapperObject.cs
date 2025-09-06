using Godot;

namespace Ciallo.Command;

public partial class CommandWrapperObject(CommandBase command) : GodotObject 
{
    public override void _Notification(int what)
    {
        if(what == NotificationPredelete)
        {
            command.FreeGodotObject();
        }
    }
    
    public void Do() => command.Do();
    public void Undo() => command.Undo();
}