using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Ciallo.Widget;

[GlobalClass, Icon("res://Icon/tune.svg")]
public partial class PropertyContainer : VBoxContainer
{
    public override void _EnterTree()
    {
        AddThemeConstantOverride("v_separation", 10);
    }

    public void AddPropertyControl(string name, [NotNull] Control control)
    {
        var box = new HFlowContainer()
        {
            LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.End,
        };
        box.AddThemeConstantOverride("h_separation", 30);
        box.AddThemeConstantOverride("v_separation", 0);
        box.AddChild(new Label
        {
            Text = name,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsVertical = SizeFlags.Fill,
        });
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(control);
        AddChild(box);
    }
}