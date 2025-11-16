using System.Diagnostics;
using Ciallo.Geometry;
using Ciallo.Rendering;
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

    /// A failure attempt to increase sampling rate of cursor movement, it seems that the _Process function cannot get pen position.
    /// The OnGuiInput invocation interval seems to be decided by user's device.
    // private bool _dispatchMotionByProcess = false;
    // private Vector2 _processPrevScreenPos;
    // private Vector2 _processPrevWorldPos;
    // private float _processPrevPressure;
    // private Vector2 _processPrevTilt;
    // public override void _Process(double delta)
    // {
    //     // Cannot get my pen position by this function.
    //     var screenPos = GetViewport().GetMousePosition() - GlobalPosition;
    //     var invTransform = _camera.GetViewportTransform().AffineInverse();
    //     var worldPos = invTransform * screenPos;
    //
    //     _dispatchMotionByProcess = !OS.LowProcessorUsageMode;
    //
    //     if (_dispatchMotionByProcess && !(screenPos - _processPrevScreenPos).IsZeroApprox())
    //     {
    //         var worldDelta = worldPos - invTransform * _prevScreenPos;
    //
    //         DispatchMotion(new CursorMotionData()
    //         {
    //             ScreenPosition = screenPos,
    //             ScreenDelta = screenPos - _processPrevScreenPos,
    //             WorldPosition = worldPos,
    //             WorldDelta = worldDelta,
    //             Pressure = _prevPressure,
    //             PressureDelta = _prevPressure - _processPrevPressure,
    //             Tilt = _prevTilt,
    //             TiltDelta = _prevTilt - _processPrevTilt,
    //         });
    //
    //         _processPrevPressure = _prevPressure;
    //         _processPrevTilt = _prevTilt;
    //     }
    //     _processPrevScreenPos = screenPos;
    //     _processPrevWorldPos = worldPos;
    // }

    // Following thing happens:
    // When disable low processor usage mode, and shen uses mouse, it works normally, GUI sample events at interval around 1ms
    // When using touch screen stylus, GUI input events are sampled at interval around 5-9ms, causing very noticeable input lag.
    // Invariant to project settings "Input > Mouse > Emulate Touch From Mouse"

    // Seems like shen's mouse reporting at >1000Hz, and stylus at 150Hz.
    // Need further test on some wacom devices.
    public void OnGuiInput(InputEvent e)
    {
        if (!Document.IsAlive) return; // This check prevents errors when the document is closed.
        // The container is queued for deletion but the Document entity is freed immediately, which can cause this method to be called on a disposed entity.
        if (e is InputEventKey key) DispatchKey(key);
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
            _prevPressure = motion.Pressure;
            _prevTilt = motion.Tilt;

            DispatchMotion(new CursorMotionData()
            {
                ScreenPosition = screenPos,
                ScreenDelta = screenDelta,
                WorldPosition = worldPos,
                WorldDelta = worldDelta,
                Pressure = motion.Pressure,
                PressureDelta = motion.Pressure - _prevPressure,
                Tilt = motion.Tilt,
                TiltDelta = motion.Tilt - _prevTilt,
                TimeDeltaMs = (float)_timer.Elapsed.TotalMilliseconds,
            });
            _timer.Restart();
        }

        // ------------ Canvas navigation handling -------------
        var panel = (PaintPanel)Owner;
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

    public Entity Document;
    private ToolButtonPanel ToolManager => Document.Get<ToolButtonPanel>();

    private void DispatchKey(InputEventKey key)
    {
        ToolManager.ActiveTool.Value?.OnKey(key);
    }

    public void DispatchLeftClick(CursorButtonData data)
    {
        ToolManager.ActiveTool.Value?.OnLeftClick(data);
    }

    public void DispatchLeftRelease(CursorButtonData data)
    {
        ToolManager.ActiveTool.Value?.OnLeftRelease(data);
    }

    public void DispatchMotion(CursorMotionData data)
    {
        ToolManager.ActiveTool.Value?.OnMoving(data);
        _worldCursorDetectionArea.OnCursorMove(data);
    }

    public void DispatchRightClick(CursorButtonData data)
    {
        ToolManager.ActiveTool.Value?.OnRightClick(data);
    }

    public void DispatchRightRelease(CursorButtonData data)
    {
        ToolManager.ActiveTool.Value?.OnRightRelease(data);
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