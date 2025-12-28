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
            DispatchKey(key, new()
            {
                ScreenPosition = _prevScreenPos,
                WorldPosition = _prevWorldPos,
                Tilt = _prevTilt,
            });
        }
        // Following code only deal with cursor events.
        // Note: Godot treats stylus pen input as mouse input.
        if (e is not InputEventMouse mouseEvent) return;

        var screenPos = mouseEvent.Position;
        var screenDelta = screenPos - _prevScreenPos;
        var invTransform = _camera.GetViewportTransform().AffineInverse();
        var worldPos = invTransform * mouseEvent.Position;
        var prevWorldPosWithCurrentCamera = invTransform * _prevScreenPos;
        var worldDelta = worldPos - prevWorldPosWithCurrentCamera;

        _prevScreenPos = screenPos;
        _prevWorldPos = worldPos;

        if (mouseEvent is InputEventMouseButton mouseButton && !_isPanning)
        {
            DispatchMouseButton(mouseButton, new()
            {
                ScreenPosition = _prevScreenPos,
                WorldPosition = _prevWorldPos,
                Tilt = _prevTilt,
            });
        }

        if (mouseEvent is InputEventMouseMotion motion)
        {
            var currentPressure = AppPreference.PenPressureRemapCurve.SampleX(motion.Pressure);

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
                TimeDelta = _timer.Elapsed,
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
            _isPanning = true;
        }
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: false })
        {
            _isPanning = false;
        }

        // Double click to reset camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.Middle, DoubleClick: true })
        {
            panel.Offset.Value = Vector2.Zero;
            panel.Zoom.Value = 1.0f;
            panel.CanvasRotation.Value = 0.0f;
        }
        // Scroll mouse wheel to zoom camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, AltPressed: false } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f + AppPreference.MouseWheelZoomFactor.Value;
            // Dirty patch to fix when mouse scroll zooming, the hover area is not updated correctly.
            var newWorldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
            _worldCursorDetectionArea.UpdateHovering(newWorldPos);
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, AltPressed: false } && _isHovering)
        {
            panel.Zoom.Value *= 1.0f - AppPreference.MouseWheelZoomFactor.Value;

            var newWorldPos = _camera.GetViewportTransform().AffineInverse() * mouseEvent.Position;
            _worldCursorDetectionArea.UpdateHovering(newWorldPos);
        }

        // Alt + scroll mouse wheel to rotate camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, AltPressed: true } && _isHovering)
        {
            panel.CanvasRotation.Value += AppPreference.MouseWheelRotateFactor.Value;
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, AltPressed: true } && _isHovering)
        {
            panel.CanvasRotation.Value -= AppPreference.MouseWheelRotateFactor.Value;
        }
    }

    public Entity Document;
    private ToolManager ToolManager => Document.Get<ToolManager>();

    private void DispatchKey(InputEventKey key, CursorButtonData data)
    {
        if (ToolManager.ActiveTool.Value?.OnKey(key, data) == true)
            GetViewport().SetInputAsHandled();
    }

    private void DispatchMouseButton(InputEventMouseButton mouse, CursorButtonData data)
    {
        if (ToolManager.ActiveTool?.Value.OnMouseButton(mouse, data) == true)
            GetViewport().SetInputAsHandled();
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