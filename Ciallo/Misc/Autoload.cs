using Godot;
using MemoryPack;
using Newtonsoft.Json;
using ObservableCollections;
using R3;

namespace Ciallo.Misc;

public partial class Autoload : Node
{
    public override void _EnterTree()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ObservableList<>), typeof(ObservableListFormatter<>));
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = 
            {
                
            },
            TypeNameHandling = TypeNameHandling.Auto,
        };
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            Preferences.Save();
    }

    public override void _Ready()
    {
        
    }
}