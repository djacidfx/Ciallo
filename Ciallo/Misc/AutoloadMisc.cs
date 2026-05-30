using Godot;
using System.Runtime;


namespace Ciallo;

public partial class AutoloadMisc : Node
{
    public override void _EnterTree()
    {
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
    }

    public override void _Notification(int what) { }

    public override void _Ready() { }

    public override void _ExitTree()
    {

    }
}