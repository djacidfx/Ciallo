using System.Diagnostics;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Tool;
using Frent;
using Godot;

namespace Ciallo.NodeControl;

/// <summary>
/// Responsible for collecting and dispatching canvas gui input events.
/// Current version also handles canvas navigation with mouse wheel. May change in the future.
/// </summary>
public partial class WorldEventDispatcher : SubViewportContainer
{
    private Camera2D _camera;
    private WorldCursorDetectionArea _worldCursorDetectionArea;

    private bool _isHovering = false;
    private bool _isPanning = false;
    private bool _isInteracting;
    private Vector2 _prevScreenPos;
    private Vector2 _prevWorldPos;
    private float _prevPressure;
    private Vector2 _prevTilt;

    private Stopwatch _timer;

    public override void _Ready()
    {
        _timer = Stopwatch.StartNew();

        _camera = GetNode<Camera2D>("%MainCamera");
        _worldCursorDetectionArea = GetNode<WorldCursorDetectionArea>("%WorldCursorDetectionArea");

        GuiInput += OnGuiInput;
        MouseEntered += OnMouseEnter;
        MouseExited += OnMouseExit;
    }

    public void OnGuiInput(InputEvent e)
    {
        if (!Document.IsAlive) return; // This check prevents errors when the document is closed.
        // The container is queued for deletion but the Document entity is freed immediately, which can cause this method to be called on a disposed entity.

        // ------------ Tool events handling -------------
        if (e is InputEventKey key)
        {
            DispatchKey(key);
            if (_isInteracting) GetViewport().SetInputAsHandled();
        }
        // Only deal with cursor events
        if (e is not InputEventMouse mouseEvent) return;

        var screenPos = mouseEvent.Position;
        var screenDelta = screenPos - _prevScreenPos;
        var invTransform = _camera.GetViewportTransform().AffineInverse();
        var worldPos = invTransform * mouseEvent.Position;
        var prevWorldPosWithCurrentCamera = invTransform * _prevScreenPos;
        var worldDelta = worldPos - prevWorldPosWithCurrentCamera;

        _prevScreenPos = screenPos;
        _prevWorldPos = worldPos;

        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } lClick && _isHovering && !_isPanning)
        {
            DispatchLeftClick(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                Tilt = _prevTilt,
            });
        }

        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } lRelease)
        {
            DispatchLeftRelease(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                Tilt = _prevTilt,
            });
        }

        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } rClick && _isHovering && !_isPanning)
        {
            DispatchRightClick(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                Tilt = _prevTilt,
            });
        }

        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } rRelease)
        {
            DispatchRightRelease(new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                Tilt = _prevTilt,
            });
        }

        if (mouseEvent is InputEventMouseMotion motion)
        {
            var currentPressure = AppPreference.PenPressureRemapCurve.SampleX(motion.Pressure);
            var elapsed = _timer.Elapsed;

            DispatchMotion(new()
            {
                ScreenPosition = screenPos,
                ScreenDelta = screenDelta,
                WorldPosition = worldPos,
                WorldDelta = worldDelta,
                Pressure = currentPressure,
                PressureDelta = currentPressure - _prevPressure,
                Tilt = motion.Tilt,
                TiltDelta = motion.Tilt - _prevTilt,
                TimeDelta = elapsed,
            });

            _prevPressure = currentPressure;
            _prevTilt = motion.Tilt;

            _timer.Restart();
        }

        // ------------ Canvas navigation handling -------------
        var panel = (PaintPanel)Owner;
        if (mouseEvent is InputEventMouseMotion && _isPanning) panel.Offset.Value -= worldDelta;

        // Drag middle mouse to pan
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: true } && _isHovering)
        {
            _isInteracting = true;
            _isPanning = true;
        }
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: false })
        {
            _isPanning = false;
            _isInteracting = false;
        }

        // Double click to reset camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, DoubleClick: true })
        {
            panel.Offset.Value = Vector2.Zero;
            panel.Zoom.Value = 1.0f;
            panel.CanvasRotation.Value = 0.0f;
        }
        // Scroll mouse wheel zooming.
        var zoomFactor = AppPreference.MouseWheelZoomFactor.Value;
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f + zoomFactor;
            // Dirty patch to fix when mouse scroll zooming, the hover area is not updated correctly.
            var newWorldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
            _worldCursorDetectionArea.UpdateHovering(newWorldPos);
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f - zoomFactor;

            var newWorldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
            _worldCursorDetectionArea.UpdateHovering(newWorldPos);
        }

        // ------------ Other -------------
        if (_isInteracting) GetViewport().SetInputAsHandled();
    }

    public Entity Document;
    private ToolManager ToolManager => Document.Get<ToolManager>();

    private void DispatchKey(InputEventKey key)
    {
        var toolAction = ToolManager.ActiveTool.Value?.OnKey(key);
        if (toolAction.HasValue && toolAction.Value.HasFlag(ToolKeyActions.HandleInput))
            _isInteracting = toolAction.Value.HasFlag(ToolKeyActions.Interact);
    }

    public void DispatchLeftClick(CursorButtonData data)
    {
        _isInteracting = ToolManager.ActiveTool.Value?.OnLeftClick(data) == true;
    }

    public void DispatchLeftRelease(CursorButtonData data)
    {
        _isInteracting = ToolManager.ActiveTool.Value?.OnLeftRelease(data) == true;
    }

    public void DispatchRightClick(CursorButtonData data)
    {
        _isInteracting = ToolManager.ActiveTool.Value?.OnRightClick(data) == true;
    }

    public void DispatchRightRelease(CursorButtonData data)
    {
        _isInteracting = ToolManager.ActiveTool.Value?.OnRightRelease(data) == true;
    }

    public void DispatchMotion(CursorMotionData data)
    {
        ToolManager.ActiveTool.Value?.OnMoving(data);
        _worldCursorDetectionArea.UpdateHovering(data.WorldPosition);
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