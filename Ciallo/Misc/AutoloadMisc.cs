using Godot;

namespace Ciallo.Misc;

public partial class AutoloadMisc : Node
{
    public override void _EnterTree()
    {
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }

    public override void _Notification(int what)
    {
    }

    public override void _Ready()
    {
    }
}