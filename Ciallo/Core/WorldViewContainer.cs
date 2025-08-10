using Godot;
using System;
using R3;


namespace Ciallo.Core;

public partial class WorldViewContainer : SubViewportContainer
{
    private Camera2D _camera;
    private bool _isHovering = false;
    
    public override void _Ready()
    {
        _camera = this.GetChild(0).GetChild<Camera2D>(1);
    }

    public void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mouseEvent)
        {
            var worldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
        }
    }
    
    public void OnMouseEnter()
    {
        // Pitfall: Dragging the vsplit/hsplit bar around the container can trigger mouse enter.
        // So use OnGuiInput together to decide whether handle world input.
        _isHovering = true;
    }
    
    public void OnMouseExit()
    {
        _isHovering = false;
    }
}
