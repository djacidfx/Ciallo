using System.Diagnostics.CodeAnalysis;
using Godot;
using R3;

namespace Ciallo.Misc;

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
        if (!Engine.IsEditorHint()) return;
        // Add dummy properties to show in the godot editor
        var dummy1 = new Label { Text = $"I'm a {(Horizontal? "horizontal":"vertical")} property container" };
        var control1 = new CheckBox();
        var hbox1 = new HBoxContainer();
        hbox1.AddChild(dummy1);
        hbox1.AddChild(control1);
        this.AddChild(hbox1);
            
        this.AddChild(new VSeparator());
        
        var dummy2 = new Label { Text = "Dummy property" };
        var control2 = new OptionButton();
        var hbox2 = new HBoxContainer();
        hbox2.AddChild(dummy2);
        hbox2.AddChild(control2);
        this.AddChild(hbox2);
    }

    public override void _Ready()
    {
        
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint()) return;
        foreach (Node child in GetChildren())
        {
            if (child is HBoxContainer || child is VSeparator)
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }
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