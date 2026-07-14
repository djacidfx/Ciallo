#if TOOLS
using Godot;

namespace Ciallo.Widget;

[Tool]
public partial class DockableContainerPlugin : EditorPlugin
{
    private DockableLayoutInspectorPlugin _layoutInspectorPlugin;

    public override void _EnterTree()
    {
        _layoutInspectorPlugin = new DockableLayoutInspectorPlugin();
        AddInspectorPlugin(_layoutInspectorPlugin);
    }

    public override void _ExitTree()
    {
        RemoveInspectorPlugin(_layoutInspectorPlugin);
        _layoutInspectorPlugin = null;
    }
}
#endif
