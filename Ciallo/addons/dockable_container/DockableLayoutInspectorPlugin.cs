#if TOOLS
using Godot;

namespace Ciallo.Widget.DockableContainer;

public partial class DockableLayoutInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object) => @object is DockableContainer;

    public override bool _ParseProperty(
        GodotObject @object,
        Variant.Type type,
        string name,
        PropertyHint hintType,
        string hintString,
        PropertyUsageFlags usageFlags,
        bool wide
    )
    {
        // Must be capitalized
        if (name != "Layout") return false;

        AddPropertyEditor(name, new DockableLayoutEditorProperty());
        return false;
    }
}
#endif
