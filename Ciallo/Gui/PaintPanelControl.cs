using Godot;
using System;
using System.Collections.Generic;
using Ciallo.Core;
using Ciallo.Misc;
using R3;

namespace Ciallo.Gui;

public partial class PaintPanelControl : PanelContainer
{
    public Camera2D Camera;

    public override void _Ready()
    {
        Camera = GetNode<Camera2D>("%Camera2D")
                 ?? throw new NullReferenceException("Camera2D not found.");
        var zoomControl = GetNode<SliderSpinBoxPair>("%ZoomControl") 
                          ?? throw new NullReferenceException("ZoomControl not found.");
        var rotationControl = GetNode<SliderSpinBoxPair>("%RotationControl")
                              ?? throw new NullReferenceException("RotationControl not found.");
        zoomControl.ValueChanged += newZoom =>
        {
            Camera.Zoom = Vector2.One * (float)newZoom; 
        };
        rotationControl.ValueChanged += newRotation =>
        {
            // Negative since setting "camera rotation" is inverted to "canvas rotation".
            // Canvas rotation is more intuitive to users.
            Camera.Rotation = -Mathf.DegToRad((float)newRotation);
        };
    }
}
