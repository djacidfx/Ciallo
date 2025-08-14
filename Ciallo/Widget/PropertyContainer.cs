using System.Diagnostics.CodeAnalysis;
using Godot;
using R3;

namespace Ciallo.Widget;

[GlobalClass, Tool, Icon("res://Icons/tune.svg")]
public partial class PropertyContainer : BoxContainer
{
    [Export] public bool Horizontal
    {
        get => !this.Vertical;
        set => this.Vertical = !value;
    }

    public PropertyContainer()
    {
        Horizontal = false;
    }

    public PropertyContainer(bool isHorizontal = false)
    {
        Horizontal = isHorizontal;
    }

    public override void _EnterTree()
    {
        
    }

    public override void _Ready()
    {
        
    }

    public override void _ExitTree()
    {
        
    }
    
    public void AddPropertyControl(string name, [NotNull] Control control)
    {
        var box = new HBoxContainer();
        box.AddChild(new Label { Text = name });
        box.AddChild(control);
        this.AddChild(box);
        if(Horizontal) this.AddChild(new VSeparator());
    }
}