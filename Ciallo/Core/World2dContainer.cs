using Godot;
using System;
using Windows.Win32;
using R3;


namespace Ciallo.Core;

public partial class World2dContainer : SubViewportContainer
{
    private Camera2D _camera;
    private bool _isHovering = false;
    
    public override void _Ready()
    {
        _camera = this.GetChild(0).GetChild<Camera2D>(1);
    }

    public override void _Process(double delta)
    {
        
    }

    public void OnGuiInput(InputEvent e)
    {
        
    }

    public override void _Input(InputEvent e)
    {
        
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
