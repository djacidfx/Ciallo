using System.Diagnostics;
using Ciallo.Geometry;
using Ciallo.Tool;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Responsible for collecting and dispatching canvas gui input events.
/// Current version also handles canvas navigation with mouse wheel. May change in the future.
/// </summary>
public partial class WorldEventDispatcher : SubViewportContainer
{
    private Camera2D _camera;

    private bool _isHovering;
    private bool _isPanning;

    private Vector2 _prevScreenPos;
    private Vector2 _prevWorldPos;
    private float _prevPressure;
    private Vector2 _prevTilt;

    private Stopwatch _timer;

    public Entity Document;
    private ToolManager ToolManager => Document.Get<ToolManager>();

    public override void _Ready()
    {
        _timer = Stopwatch.StartNew();

        _camera = GetNode<Camera2D>("%MainCamera");

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
        }
        // Following code only deal with cursor events.
        // Note: Godot treats stylus pen input as mouse input.
        if (e is not InputEventMouse mouseEvent) return;

        // ------------ Canvas navigation handling -------------
        // Pitfall _camera.GetViewportTransform() doesn't update until the end of frame, so not response to property change.
        var screenPos = mouseEvent.Position;
        var screenDelta = screenPos - _prevScreenPos;
        var invTransform = _camera.GetViewportTransform().AffineInverse();
        var worldPos = invTransform * screenPos;
        var prevWorldPosWithCurrentCamera = invTransform * _prevScreenPos;
        var worldDeltaBeforeTransformCamera = worldPos - prevWorldPosWithCurrentCamera;
        var worldDelta = worldPos - _prevWorldPos;

        var panel = (PaintPanel)Owner;
        if (mouseEvent is InputEventMouseMotion && _isPanning)
            panel.CameraOffset.Value -= worldDeltaBeforeTransformCamera;

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
            panel.CameraOffset.Value = Vector2.Zero;
            panel.CameraZoom.Value = 1.0f;
            panel.CameraRotation.Value = 0.0f;
        }

        // Scroll mouse wheel to zoom camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, AltPressed: false } && _isHovering)
        {
            panel.CameraZoom.Value *= 1.0f + AppPreference.MouseWheelZoomFactor.Value;
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, AltPressed: false } && _isHovering)
        {
            panel.CameraZoom.Value *= 1.0f - AppPreference.MouseWheelZoomFactor.Value;
        }

        // Alt + scroll mouse wheel to rotate camera.
        if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, AltPressed: true } && _isHovering)
        {
            panel.CameraRotation.Value += AppPreference.MouseWheelRotateFactor.Value;
        }
        else if (mouseEvent is InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, AltPressed: true } && _isHovering)
        {
            panel.CameraRotation.Value -= AppPreference.MouseWheelRotateFactor.Value;
        }

        // ------------- Events dispatch -------------------------
        if (mouseEvent is InputEventMouseButton mouseButton && !_isPanning)
        {
            DispatchMouseButton(mouseButton, new()
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
                Tilt = _prevTilt,
            });
        }

        if (!worldDelta.IsZeroApprox()) // dispatch motion
        {
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
            }
            else
            {
                // Motion without InputEventMouseMotion, e.g. mouse wheel zoom causing world position change.
                // dispatch with previous pressure and tilt.
                DispatchMotion(new()
                {
                    ScreenPosition = screenPos,
                    ScreenDelta = screenDelta,
                    WorldPosition = worldPos,
                    WorldDelta = worldDelta,
                    Pressure = _prevPressure,
                    PressureDelta = 0,
                    Tilt = _prevTilt,
                    TiltDelta = Vector2.Zero,
                    TimeDelta = _timer.Elapsed,
                });
            }
        }

        _timer.Restart();
        _prevScreenPos = screenPos;
        _prevWorldPos = worldPos;
    }

    private void DispatchKey(InputEventKey key)
    {
        if (ToolManager.ActiveTool.Value?.OnKey(key) == true)
            GetViewport().SetInputAsHandled();
    }

    private void DispatchMouseButton(InputEventMouseButton mouse, CursorButtonData data)
    {
        ToolManager.ActiveTool.Value?.OnMouseButton(mouse, data);
    }

    public void DispatchMotion(CursorMotionData data)
    {
        ToolManager.ActiveTool.Value?.OnMoving(data);
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