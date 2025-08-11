using Godot;
using System;
using Ciallo.Gui;


namespace Ciallo.Core;

/// <summary>
/// Responsible for collecting and dispatching canvas gui input events.
/// Current version also handles canvas navigation with mouse wheel. May change in the future.
/// </summary>
public partial class WorldViewContainer : SubViewportContainer
{
    private Camera2D _camera;
    private SliderSpinBoxPair _zoomControl;
    private SliderSpinBoxPair _rotationControl;
    
    private bool _isHovering = false;
    private bool _isPanning = false;
    private Vector2 _prevScreenPos;
    private Vector2 _prevWorldPos;
    
    public override void _Ready()
    {
        _camera = GetChild(0).GetChild<Camera2D>(1);
        _zoomControl = GetNode<SliderSpinBoxPair>("%ZoomControl")
                       ?? throw new NullReferenceException("ZoomControl not found.");
        _rotationControl = GetNode<SliderSpinBoxPair>("%RotationControl")
                           ?? throw new NullReferenceException("RotationControl not found.");
    }
    
    public void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            var worldPos = _camera.GetViewportTransform().AffineInverse() * motion.Position;
            var screenPos = motion.Position;
            var prevWorldPosWithCurrentCamera = _camera.GetViewportTransform().AffineInverse() * _prevScreenPos;
            var screenDelta = screenPos - _prevScreenPos;
            var worldDelta = worldPos - prevWorldPosWithCurrentCamera;
            var data = new CursorMotionData
            {
                ScreenPosition = screenPos,
                ScreenDelta = screenDelta,
                WorldPosition = worldPos,
                WorldDelta = worldDelta,
                RawData = motion
            };
            
            Dispatch(data);
            
            _prevScreenPos = screenPos;
            _prevWorldPos = worldPos;
            
            if (_isPanning)
            {
                _camera.Position -= worldDelta;
            }
        }

        // Handle 
        // Drag middle mouse to pan
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: true } && _isHovering)
            _isPanning = true;
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: false })
            _isPanning = false;
        
        // Double click to reset camera position.
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Middle, DoubleClick: true })
        {
            _camera.Position = Vector2.Zero;
        }
        // Scroll mouse wheel zooming.
        var zoomFactor = ProgramPreferences.MouseWheelZoomFactor.Value;
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp } && _isHovering)
        {
            _zoomControl.Value *= 1.0f + zoomFactor;
        }
        else if (e is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown } && _isHovering)
        {
            _zoomControl.Value *= 1.0f - zoomFactor;
        }
    }
    
    public void OnMouseEnter()
    {
        // Pitfall: Godot Bug 4.4.1, Dragging the vsplit/hsplit bar around the container can trigger mouse enter.
        // So use OnGuiInput together to decide whether handle world input.
        _isHovering = true;
    }
    
    public void OnMouseExit()
    {
        _isHovering = false;
    }

    public void Dispatch(CursorMotionData data)
    {
        
    }
}
