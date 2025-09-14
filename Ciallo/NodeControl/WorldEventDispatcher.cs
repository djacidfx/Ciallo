using Godot;
using System;
using Ciallo.Command;
using Ciallo.Tool;
using Ciallo.Widget;

namespace Ciallo.NodeControl;

/// <summary>
/// Responsible for collecting and dispatching canvas gui input events.
/// Current version also handles canvas navigation with mouse wheel. May change in the future.
/// </summary>
public partial class WorldEventDispatcher : SubViewportContainer
{
    private Camera2D _camera;
    
    private bool _isHovering = false;
    private bool _isPanning = false;
    private Vector2 _prevScreenPos;
    private Vector2 _prevWorldPos;
    private float _prevPressure;
    private Vector2 _prevTilt;
    
    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("%Camera2D");
    }
    
    public void OnGuiInput(InputEvent e)
    {
        if (e is InputEventKey key) DispatchKey(key);
        if (e is not InputEventMouse mouseEvent) return;
        
        var screenPos = mouseEvent.Position;
        var screenDelta = screenPos - _prevScreenPos;
        var worldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
        var prevWorldPosWithCurrentCamera = _camera.GetViewportTransform().AffineInverse() * _prevScreenPos;
        var worldDelta = worldPos - prevWorldPosWithCurrentCamera;
        
        _prevScreenPos = screenPos;
        _prevWorldPos = worldPos;
        
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } lClick && _isHovering)
        {
            DispatchLeftClick(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                RawData = lClick
            });
        }
        
        if(mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } lRelease)
        {
            DispatchLeftRelease(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                RawData = lRelease
            });
        }
        
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } rClick && _isHovering)
        {
            DispatchRightClick(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                RawData = rClick
            });
        }
        
        if(mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } rRelease)
        {
            DispatchRightRelease(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                RawData = rRelease
            });
        }
        
        var panel = (PaintPanel)Owner;
        if (mouseEvent is InputEventMouseMotion motion)
        {
            var data = new CursorMotionData()
            {
                ScreenPosition = screenPos,
                ScreenDelta = screenDelta,
                WorldPosition = worldPos,
                WorldDelta = worldDelta,
                Pressure = motion.Pressure,
                PressureDelta = motion.Pressure - _prevPressure,
                Tilt = motion.Tilt,
                TiltDelta = motion.Tilt - _prevTilt,
                RawData = motion
            };
            
            _prevPressure = motion.Pressure;
            _prevTilt = motion.Tilt;
            
            DispatchMotion(data);
        }
        
        // ------------ Canvas navigation handling -------------
        if (mouseEvent is InputEventMouseMotion && _isPanning) panel.Offset.Value -= worldDelta;
        
        // Drag middle mouse to pan
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: true } && _isHovering) _isPanning = true;
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: false }) _isPanning = false;
        
        // Double click to reset camera position.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, DoubleClick: true })
        {
            panel.Offset.Value = Vector2.Zero;
        }
        // Scroll mouse wheel zooming.
        var zoomFactor = AppPreference.MouseWheelZoomFactor.Value;
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f + zoomFactor;
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f - zoomFactor;
        }
    }

    private void DispatchKey(InputEventKey key)
    {
        AppToolManager.ActiveTool.Value?.OnKey(key);
    }

    public void DispatchLeftClick(CursorButtonData data)
    {
        AppToolManager.ActiveTool.Value?.OnLeftClick(data);
    }
    
    public void DispatchLeftRelease(CursorButtonData data)
    {
        AppToolManager.ActiveTool.Value?.OnLeftRelease(data);
    }

    public void DispatchMotion(CursorMotionData data)
    {
        AppToolManager.ActiveTool.Value?.OnMoving(data);
    }
    
    public void DispatchRightClick(CursorButtonData data)
    {
        AppToolManager.ActiveTool.Value?.OnRightClick(data);
    }
    
    public void DispatchRightRelease(CursorButtonData data)
    {
        AppToolManager.ActiveTool.Value?.OnRightRelease(data);
    }

    public void OnMouseEnter()
    {
        // Pitfall: Godot Bug 4.4.1, Dragging the vsplit/hsplit bar around the container can trigger mouse enter.
        // So use OnGuiInput together to decide whether handle world input.
        _isHovering = true;
        // For some unknown bug, shen's touch screen pen cannot trigger Godot's control's focus on pen cursor enter (mouse can).
        CallDeferred(Control.MethodName.GrabFocus);
    }

    public void OnMouseExit()
    {
        CallDeferred(Control.MethodName.ReleaseFocus);
        _isHovering = false;
    }
}
