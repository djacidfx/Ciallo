using Godot;
using System;

namespace Ciallo.Widget;

public partial class PaintPanelControl : PanelContainer
{
    public Camera2D Camera;

    public override void _Ready()
    {
        Camera = GetNode<Camera2D>("%Camera2D")
                 ?? throw new NullReferenceException("Camera2D not found.");
        var zoomControl = GetNode<SpinSlider>("%ZoomControl") 
                          ?? throw new NullReferenceException("ZoomControl not found.");
        var rotationControl = GetNode<SpinSlider>("%RotationControl")
                              ?? throw new NullReferenceException("RotationControl not found.");
        zoomControl.ValueChanged += newZoom => Camera.Zoom = Vector2.One * (float)newZoom;
        rotationControl.ValueChanged += newRotation =>
        {
            // Negative since setting "camera rotation" is inverted to "canvas rotation".
            // Canvas rotation is more intuitive to users.
            Camera.Rotation = -Mathf.DegToRad((float)newRotation);
        };
    }
}
