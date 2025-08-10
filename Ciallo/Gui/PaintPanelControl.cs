using Godot;
using System;
using System.Collections.Generic;
using Ciallo.Core;
using Ciallo.Misc;
using R3;

namespace Ciallo.Gui;

[SceneTree(root:"rt")]
public partial class PaintPanelControl : PanelContainer
{
    public SubViewport SubViewport;
    public WorldView WorldView;

    public Camera2D Camera;
    
    private readonly ReactiveProperty<float> _cameraRotationDegree = new(0f);
    private readonly ReactiveProperty<float> _cameraZoom = new(1.0f);

    public override void _Ready()
    {
        SubViewport = GetNode<SubViewport>("VBoxContainer/WorldViewContainer/SubViewport");
        WorldView = SubViewport.GetChild<WorldView>(0);
        Camera = SubViewport.GetChild<Camera2D>(1);

        var button = new OptionButton();
        button.BindValue([Viewport.Msaa.Disabled, Viewport.Msaa.Msaa2X, Viewport.Msaa.Msaa4X, Viewport.Msaa.Msaa8X], ProgramPreferences.Msaa);
        _PropertyContainer.AddPropertyControl("Anti-Aliasing: ", button);
        SubViewport.Msaa2D = ProgramPreferences.Msaa.Value;
        ProgramPreferences.Msaa.Subscribe(value => SubViewport.Msaa2D = value).AddTo(this);
        
        var rotControl = new SliderSpinBoxPair
        {
            MinValue = -180,
            MaxValue = 180,
            Step = 0.1
        };
        _PropertyContainer.AddPropertyControl("Rotation: ", rotControl);
        rotControl.BindValue(_cameraRotationDegree);
        _cameraRotationDegree.Subscribe(value => Camera.Rotation = Mathf.DegToRad(value)).AddTo(this);
        
        var zoomControl = new SliderSpinBoxPair
        {
            MinValue = 0.1,
            MaxValue = 100,
            ExpEdit = true
        };
        zoomControl.SpinBox.Step = 0.1;
        zoomControl.Slider.Step = 0;
        _PropertyContainer.AddPropertyControl("Zoom: ", zoomControl);
        // Pitfall: Changing the control's irrelevant properties may cause the control change its inner value.
        // Thus, if possible, always bind the control to the property after setting control's properties.
        zoomControl.BindValue(_cameraZoom);
        _cameraZoom.Subscribe(value => Camera.Zoom = new(value, value)).AddTo(this);
    }
}
