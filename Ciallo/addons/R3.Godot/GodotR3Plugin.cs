#if TOOLS
#nullable enable

using Godot;

namespace R3;

[Tool]
public partial class GodotR3Plugin : EditorPlugin
{
    // static ObservableTrackerDebuggerPlugin? observableTrackerDebugger;
    public override void _EnterTree()
    {
        //// Shen: Buggy in v1.3.0
        // observableTrackerDebugger ??= new ObservableTrackerDebuggerPlugin();
        // AddDebuggerPlugin(observableTrackerDebugger);
        
        //// Shen: Unfriendly to version control and CI systems to auto add these Autoloads, add them manually. 
        // Automatically install autoloads here for ease of use.
        // AddAutoloadSingleton(nameof(FrameProviderDispatcher), "res://addons/R3.Godot/FrameProviderDispatcher.cs");
        // AddAutoloadSingleton(nameof(ObservableTrackerRuntimeHook), "res://addons/R3.Godot/ObservableTrackerRuntimeHook.cs");
    }

    public override void _ExitTree()
    {
        // if (observableTrackerDebugger != null)
        // {
        //     RemoveDebuggerPlugin(observableTrackerDebugger);
        //     observableTrackerDebugger = null;
        // }
        // RemoveAutoloadSingleton(nameof(FrameProviderDispatcher));
        // RemoveAutoloadSingleton(nameof(ObservableTrackerRuntimeHook));
    }
}
#endif
