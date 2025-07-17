using Godot;
using System;
using Ciallo.Misc;
using R3;

namespace Ciallo.Core;

[SceneTree(root:"rt")]
public partial class PaintPanelControl : PanelContainer
{
    public override void _Process(double delta)
    {
        
    }

    public void OnResize()
    {
        
    }

    public override void _Ready()
    {
        var msaa = new ReactiveProperty<Viewport.Msaa>();
        var container = GetNode<HBoxContainer>("./VBoxContainer/Margin/HBoxContainer");
        var x = new PropertyEnumUI("test");
        x.Bind(msaa);
        container.AddChild(x.Control);
    }
}
