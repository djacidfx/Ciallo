using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Misc;

public partial class Autoload : Node
{
    public override void _EnterTree()
    {
        
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            ProgramPreferences.Save();
    }

    public override void _Ready()
    {
        
    }
}