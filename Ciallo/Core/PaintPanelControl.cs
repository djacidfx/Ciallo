using Godot;
using System;
using System.Collections.Generic;
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
        var container = GetNode<PropertyContainer>("./VBoxContainer/Margin/Properties");
        var button = new OptionButton();
        button.BindValue([Viewport.Msaa.Disabled, Viewport.Msaa.Msaa2X, Viewport.Msaa.Msaa4X, Viewport.Msaa.Msaa8X], ProgramPreferences.Msaa);
        container.AddPropertyControl("Anti-Aliasing: ", button);
        _SubViewport.Msaa2D = ProgramPreferences.Msaa.Value;
        ProgramPreferences.Msaa.Subscribe(value =>
        {
            _SubViewport.Msaa2D = value;
            GD.Print(_SubViewport.GetMsaa2D());
        }).AddTo(_SubViewport);
    }
}
