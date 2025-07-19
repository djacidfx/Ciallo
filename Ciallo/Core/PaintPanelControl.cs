using Godot;
using System;
using System.Collections.Generic;
using Ciallo.Misc;
using R3;

namespace Ciallo.Core;

[SceneTree(root:"rt")]
public partial class PaintPanelControl : PanelContainer
{
    public SubViewport SubViewport;
    public World2d World;

    public Camera2D Camera;
    
    private readonly ReactiveProperty<float> _cameraRotationDegree = new(0f);
    private readonly ReactiveProperty<float> _cameraZoom = new(1f);

    public override void _Ready()
    {
        SubViewport = _World2DContainer.GetChild<SubViewport>(0);
        World = SubViewport.GetChild<World2d>(0);
        Camera = SubViewport.GetChild<Camera2D>(1);

        var button = new OptionButton();
        button.BindValue([Viewport.Msaa.Disabled, Viewport.Msaa.Msaa2X, Viewport.Msaa.Msaa4X, Viewport.Msaa.Msaa8X], ProgramPreferences.Msaa);
        _PropertyContainer.AddPropertyControl("Anti-Aliasing: ", button);
        SubViewport.Msaa2D = ProgramPreferences.Msaa.Value;
        ProgramPreferences.Msaa.Subscribe(value => SubViewport.Msaa2D = value).AddTo(this);
        
        var rotControl = new SliderSpinBoxPair();
        rotControl.BindValue(_cameraRotationDegree);
        rotControl.MinValue = -180;
        rotControl.MaxValue = 180;
        rotControl.Step = 0.1;
        _PropertyContainer.AddPropertyControl("Rotation: ", rotControl);
        _cameraRotationDegree.Subscribe(value => Camera.Rotation = Mathf.DegToRad(value)).AddTo(this);
        
        var zoomControl = new SliderSpinBoxPair();
        zoomControl.BindValue(_cameraZoom);
        zoomControl.MinValue = 0.1;
        zoomControl.MaxValue = 100;
        zoomControl.SpinBox.Step = 0.1;
        zoomControl.Slider.Step = 0;
        zoomControl.Slider.ExpEdit = true;
        _PropertyContainer.AddPropertyControl("Zoom: ", zoomControl);
        _cameraZoom.Subscribe(value => Camera.Zoom = new (value, value)).AddTo(this);
    }
}
