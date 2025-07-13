using Godot;
using MemoryPack;
using ObservableCollections;
using R3;

namespace Ciallo.Misc;

public partial class Autoload : Node
{
    public override void _EnterTree()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ObservableList<>), typeof(ObservableListFormatter<>));
        
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            Godot.Autoload.Configurations.Save();
    }
}