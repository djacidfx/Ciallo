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
        // Note: If a control's CustomMinimumSize is zero, it will never be wrapped in FlowContainer.
        var box = new HFlowContainer()
        {
            Name = name,
        };
        box.AddThemeConstantOverride("h_separation", 15);
        box.AddChild(new Label
        {
            Text = name,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new(150, 0),
        });
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(control);

        var controlMinSize = control.CustomMinimumSize.Max(new Vector2(150, 0));
        control.CustomMinimumSize = controlMinSize;
        return box;
    }
}