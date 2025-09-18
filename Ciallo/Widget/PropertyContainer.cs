using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Ciallo.Widget;

[GlobalClass, Icon("res://Icon/tune.svg")]
public partial class PropertyContainer : VBoxContainer
{
    public override void _EnterTree()
    {
        AddThemeConstantOverride("separation", 20);
    }

    public Container AddPropertyControl(string name, [NotNull] Control control)
    {
        var box = CreatePropertyControl(name, control);
        AddChild(box);
        return box;
    }
    
    public static Container CreatePropertyControl(string name, [NotNull] Control control)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("v_separation", 5);
        box.AddChild(new Label
        {
            Text = name,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        });
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(control);
        return box;
    }
}