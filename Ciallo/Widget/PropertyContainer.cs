using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Ciallo.Widget;

[GlobalClass, Icon("res://Icon/tune.svg")]
public partial class PropertyContainer : VBoxContainer
{
    public override void _EnterTree()
    {
        
    }

    public void AddPropertyControl(string name, [NotNull] Control control)
    {
        var box = new HBoxContainer();
        box.AddChild(new Label
        {
            Text = name,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsVertical = SizeFlags.Fill,
        });
        control.SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand;
        box.AddChild(control);
        AddChild(box);
    }
}