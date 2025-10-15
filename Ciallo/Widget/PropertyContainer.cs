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

    public Container AddProperty(string name, [NotNull] Control control)
    {
        var box = CreatePropertyControl(name, control);
        AddChild(box);
        return box;
    }

    public Container RemoveProperty(string name)
    {
        var child = GetNode<Container>(name);
        RemoveChild(child);
        return child;
    }

    public static Container CreatePropertyControl(string name, [NotNull] Control control)
    {
        var box = new VBoxContainer()
        {
            Name = name,
        };
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