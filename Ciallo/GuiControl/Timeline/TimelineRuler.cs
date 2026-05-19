using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws frame-number ruler ticks and draggable playback-range handles.
/// </summary>
[Tool]
public partial class TimelineRuler : Control
{
    #region Export

    [Export]
    public Color PlayheadTickColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(1f, 0.65f, 0f);

    [Export]
    public Color PlaybackStartColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(0.25f, 0.85f, 0.25f, 0.9f);

    [Export]
    public Color PlaybackEndColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(0.9f, 0.25f, 0.25f, 0.9f);

    [Export]
    public int MajorTickHeight
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 16;

    [Export]
    public int MinorTickHeight
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 6;

    /// <summary>Minimum pixel gap between displayed labels.</summary>
    [Export]
    public float MinLabelSpacingPx
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 40f;

    /// <summary>Minimum pixel gap between tick marks.</summary>
    [Export]
    public float MinTickSpacingPx
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 8f;

    /// <summary>Width and height of the triangular drag handle drawn at the ruler top.</summary>
    [Export]
    public float HandleSize
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 10f;

    [Export]
    public Color TickColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(0.65f, 0.65f, 0.65f);

    /// <summary>Pixel height of the seconds band drawn above the frame-tick band.</summary>
    [Export]
    public float SecondsBandHeight
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 18f;

    #endregion

    // ── Private state ────────────────────────────────────────────────────────
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;
    private float _fps = 24f;
    private ReactiveProperty<int> _currentFrame;
    private ReactiveProperty<int> _playbackStart;
    private ReactiveProperty<int> _playbackEnd;
    private SelectionManager _selectionManager;

    private enum DragMode { None, Frame, StartHandle, EndHandle }

    private DragMode _dragMode = DragMode.None;
    private int? _hoverFrame;
    private int _frameAtDragStart;
    private int _playbackStartAtDragStart;
    private int _playbackEndAtDragStart;

    #region Theme

    public int LabelFontSize;
    public Color LabelColor;
    public Color PlaybackBackgroundColor;
    public Color OutOfPlaybackBackgroundColor;
    public Color OutOfPlaybackLabelColor;
    public Color OutOfPlaybackTickColor;
    public Color HintDotColor;

    public void InitTheme()
    {
        StyleBoxFlat styleBox;
        styleBox = (StyleBoxFlat)GetThemeStylebox("normal", "Button");
        PlaybackBackgroundColor = styleBox.BgColor;
        styleBox = (StyleBoxFlat)GetThemeStylebox("disabled", "Button");
        OutOfPlaybackBackgroundColor = styleBox.BgColor;
        LabelColor = GetThemeColor("font_color", "Label");
        LabelFontSize = (int)(0.8 * GetThemeFontSize("font_size", "Label"));
        OutOfPlaybackLabelColor = GetThemeColor("font_disabled_color", "Button");
        OutOfPlaybackTickColor = OutOfPlaybackLabelColor with { A = 0.5f };
        styleBox = (StyleBoxFlat)GetThemeStylebox("hover", "Button");
        HintDotColor = styleBox.BgColor;
    }

    #endregion

    public override void _Ready()
    {
        InitTheme();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged)
            InitTheme();

        if (what == NotificationMouseExit)
        {
            _hoverFrame = null;
            QueueRedraw();
        }
    }

    #region Setup

    /// <summary>Call once from TimelinePanel to wire zoom / scroll.</summary>
    public void Observe(ReactiveProperty<float> pixelsPerFrame, ReactiveProperty<float> scrollOffsetFrame,
        ReactiveProperty<float> fps)
    {
        pixelsPerFrame.CombineLatest(scrollOffsetFrame, (ppf, sof) => (ppf, sof * ppf))
            .Subscribe(t =>
            {
                _pixelsPerFrame = t.ppf;
                _scrollOffset = t.Item2;
                QueueRedraw();
            }).AddTo(this);
        fps.Subscribe(v =>
        {
            _fps = v;
            QueueRedraw();
        }).AddTo(this);
    }

    /// <summary>Wire the current-frame property.</summary>
    public void BindCurrentFrame(ReactiveProperty<int> currentFrame)
    {
        _currentFrame = currentFrame;
        currentFrame.Subscribe(_ => QueueRedraw()).AddTo(this);
    }

    /// <summary>Wire playback-range properties.</summary>
    public void BindPlaybackRange(ReactiveProperty<int> playbackStart, ReactiveProperty<int> playbackEnd)
    {
        _playbackStart = playbackStart;
        _playbackEnd = playbackEnd;
        playbackStart.Subscribe(_ => QueueRedraw()).AddTo(this);
        playbackEnd.Subscribe(_ => QueueRedraw()).AddTo(this);
    }

    /// <summary>Wire the selection manager for working-layer switching on frame change.</summary>
    public void BindSelectionManager(SelectionManager sm)
    {
        _selectionManager = sm;
    }

    #endregion

    // ── Coordinate helpers ───────────────────────────────────────────────────

    /// <summary>Ruler-local pixel X of a frame's left edge. Frame 0 is the virtual origin.</summary>
    private float FrameToX(int frame) => frame * _pixelsPerFrame - _scrollOffset;

    /// <summary>Frame index whose left edge is at or before ruler-local X. May return 0 or negative.</summary>
    private int XToFrame(float x) => (int)((x + _scrollOffset) / _pixelsPerFrame);

    // ── Handle hit-tests ─────────────────────────────────────────────────────

    /// <summary>Horizontal pixel tolerance added on each side of a handle's bounding box.</summary>
    private const float HandleHitTolerance = 2f;

    /// <summary>
    /// Start handle: right-angle triangle, right vertical edge at startX, body to the LEFT.
    /// Hit zone covers the triangle bounding box with a small horizontal tolerance.
    /// </summary>
    private bool HitStartHandle(Vector2 pos)
    {
        if (_playbackStart == null) return false;
        float startX = FrameToX(_playbackStart.Value);
        return pos.X >= startX - HandleSize - HandleHitTolerance &&
               pos.X <= startX + HandleHitTolerance &&
               pos.Y >= 0f && pos.Y <= Size.Y;
    }

    /// <summary>
    /// End handle: right-angle triangle, left vertical edge at endX, body to the RIGHT.
    /// </summary>
    private bool HitEndHandle(Vector2 pos)
    {
        if (_playbackEnd == null) return false;
        float endX = FrameToX(_playbackEnd.Value);
        return pos.X >= endX - HandleHitTolerance &&
               pos.X <= endX + HandleSize + HandleHitTolerance &&
               pos.Y >= 0f && pos.Y <= Size.Y;
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (_pixelsPerFrame <= 0f) return;

        float w = Size.X;
        float h = Size.Y;

        // ── Background ────────────────────────────────────────────────────────
        if (_playbackStart != null && _playbackEnd != null)
        {
            float startX = FrameToX(_playbackStart.Value);
            float endX = FrameToX(_playbackEnd.Value);

            // Out-of-range left
            if (startX > 0f)
                DrawRect(new Rect2(0f, 0f, Mathf.Min(startX, w), h), OutOfPlaybackBackgroundColor);

            // In-range
            float clampedStart = Mathf.Clamp(startX, 0f, w);
            float clampedEnd = Mathf.Clamp(endX, 0f, w);
            if (clampedEnd > clampedStart)
                DrawRect(new Rect2(clampedStart, 0f, clampedEnd - clampedStart, h), PlaybackBackgroundColor);

            // Out-of-range right
            float rightStart = Mathf.Max(0f, endX);
            if (rightStart < w)
                DrawRect(new Rect2(rightStart, 0f, w - rightStart, h), OutOfPlaybackBackgroundColor);
        }
        else
        {
            DrawRect(new Rect2(0f, 0f, w, h), OutOfPlaybackBackgroundColor);
        }

        var font = GetThemeDefaultFont();
        // Frame 0 is at virtual x=0; include a 1-frame buffer left of the view.
        int startFrame = (int)(_scrollOffset / _pixelsPerFrame) - 1;
        int endFrame = (int)((_scrollOffset + w) / _pixelsPerFrame) + 2;

        // ── Seconds band (top) ───────────────────────────────────────────────
        // Band occupies y ∈ [0, SecondsBandHeight); frame band is below.
        if (_fps > 0f)
        {
            float pxPerSecond = _pixelsPerFrame * _fps;

            // Find the smallest step (in whole seconds) so labels don't overlap.
            int secondStep = 1;
            if (pxPerSecond > 0f)
                while (pxPerSecond * secondStep < MinLabelSpacingPx)
                    secondStep++;

            int startSecond = Mathf.Max(0, (int)(_scrollOffset / pxPerSecond));
            int endSecond = (int)((_scrollOffset + w) / pxPerSecond) + 2;

            // Separator line between the two bands
            DrawLine(new Vector2(0f, SecondsBandHeight), new Vector2(w, SecondsBandHeight),
                TickColor with { A = 0.4f });

            for (int s = startSecond; s <= endSecond; s++)
            {
                if (s % secondStep != 0) continue;

                // frame for second s: frame 0 = 0 s, frame (s*Fps) = s seconds
                float x = FrameToX((int)(s * _fps));
                if (x < -pxPerSecond || x > w + pxPerSecond) continue;

                int secondFrame = (int)(s * _fps);
                bool inRange = _playbackStart != null && _playbackEnd != null
                                                      && secondFrame >= _playbackStart.Value && secondFrame < _playbackEnd.Value;
                Color secTickColor = inRange ? TickColor : OutOfPlaybackTickColor;
                Color secLabelColor = inRange ? LabelColor : OutOfPlaybackLabelColor;

                DrawLine(new Vector2(x, 0f), new Vector2(x, SecondsBandHeight), secTickColor);
                DrawString(font, new Vector2(x + 2f, SecondsBandHeight - 2f),
                    $"{s}s", HorizontalAlignment.Left, -1, LabelFontSize, secLabelColor);
            }
        }

        // ── Frame ticks & labels (bottom band) ──────────────────────────────
        // Minor-tick step: smallest interval keeping ticks ≥ MinTickSpacingPx apart.
        int tickStep = 1;
        while (_pixelsPerFrame * tickStep < MinTickSpacingPx) tickStep *= tickStep < 5 ? 5 : 2;

        // Major ticks every 5 frames (or next multiple of 5 that covers tickStep).
        int majorStep = 5;
        while (majorStep < tickStep) majorStep += 5;

        // Labels only when the major-tick pixel gap is roomy enough.
        bool showFrameLabels = _pixelsPerFrame * majorStep >= MinLabelSpacingPx;

        for (int frame = startFrame; frame <= endFrame; frame++)
        {
            bool isTickFrame = frame == 1 || frame % tickStep == 0;
            if (!isTickFrame) continue;

            float x = FrameToX(frame);
            if (x < -_pixelsPerFrame || x > w + _pixelsPerFrame) continue;

            bool isMajor = frame == 0 || frame % majorStep == 0;
            float tickH = isMajor ? MajorTickHeight : MinorTickHeight;

            bool inRange = _playbackStart != null && _playbackEnd != null && frame >= _playbackStart.Value && frame < _playbackEnd.Value;
            Color frameTickColor = inRange ? TickColor : OutOfPlaybackTickColor;
            Color frameLabelColor = inRange ? LabelColor : OutOfPlaybackLabelColor;

            DrawLine(new Vector2(x, h - tickH), new Vector2(x, h), frameTickColor);

            // Frame 0 has no visible label — numbering goes …-2, -1, (silent 0), 1, 2…
            if (isMajor && showFrameLabels && frame != 0)
                DrawString(font, new Vector2(x + 2f, h - tickH - 2f),
                        frame.ToString(), HorizontalAlignment.Left, -1, LabelFontSize, frameLabelColor);
        }

        // ── Playhead highlight ───────────────────────────────────────────────
        if (_currentFrame != null)
        {
            float px = FrameToX(_currentFrame.Value);
            if (px >= -_pixelsPerFrame && px <= w + _pixelsPerFrame)
            {
                DrawLine(new Vector2(px, 0f), new Vector2(px, h), PlayheadTickColor, 2f);
                DrawLine(new Vector2(px + _pixelsPerFrame, 0f), new Vector2(px + _pixelsPerFrame, h),
                    PlayheadTickColor with { A = 0.4f });
            }
        }

        // ── Hover hint ───────────────────────────────────────────────────────
        if (_hoverFrame != null && _dragMode == DragMode.None)
        {
            // Center of the hovered frame's pixel slot
            float hx = FrameToX(_hoverFrame.Value) + _pixelsPerFrame * 0.5f;

            // Dot vertically centered within the minor-tick height
            float dotY = h - MinorTickHeight * 0.5f;
            DrawCircle(new Vector2(hx, dotY), 6f, HintDotColor);

            // Frame label in the seconds band, centered on the same X as the dot.
            // Display _hoverFrame + 1: the region left of tick N belongs to "frame N" (1-based).
            string hoverLabel = (_hoverFrame.Value + 1).ToString();
            float labelW = font.GetStringSize(hoverLabel, HorizontalAlignment.Left, -1, LabelFontSize).X;
            DrawString(font, new Vector2(hx - labelW * 0.5f, SecondsBandHeight - 2f),
                hoverLabel, HorizontalAlignment.Left, -1, LabelFontSize, LabelColor);
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            if (btn.Pressed)
            {
                // Priority: handles first, then playhead
                if (HitStartHandle(btn.Position))
                {
                    _dragMode = DragMode.StartHandle;
                    _playbackStartAtDragStart = _playbackStart.Value;
                    _frameAtDragStart = _currentFrame?.Value ?? 0;
                }
                else if (HitEndHandle(btn.Position))
                {
                    _dragMode = DragMode.EndHandle;
                    _playbackEndAtDragStart = _playbackEnd.Value;
                    _frameAtDragStart = _currentFrame?.Value ?? 0;
                }
                else if (_currentFrame != null)
                {
                    _dragMode = DragMode.Frame;
                    _frameAtDragStart = _currentFrame.Value;
                    _currentFrame.Value = FrameFromX(btn.Position.X);
                }

                if (_dragMode != DragMode.None) AcceptEvent();
            }
            else
            {
                if (_dragMode == DragMode.Frame)
                {
                    var cmd = new CommandBuilder()
                        .SetProperty(_currentFrame, _frameAtDragStart, _currentFrame.Value);
                    if (_currentFrame.Value != _frameAtDragStart)
                    {
                        var newWorkingLayer = GetNewWorkingLayerAfterFrameChange(_frameAtDragStart, _currentFrame.Value);
                        if (!newWorkingLayer.IsNull)
                            cmd.SetTarget(newWorkingLayer).SetWorkingLayer();
                    }
                    cmd.CommitToLatest();
                }
                else if (_dragMode == DragMode.StartHandle)
                {
                    var cmd = new CommandBuilder()
                        .SetProperty(_playbackStart, _playbackStartAtDragStart, _playbackStart.Value);
                    if (_currentFrame != null && _currentFrame.Value != _frameAtDragStart)
                    {
                        cmd.SetProperty(_currentFrame, _frameAtDragStart, _currentFrame.Value);
                        var newWorkingLayer = GetNewWorkingLayerAfterFrameChange(_frameAtDragStart, _currentFrame.Value);
                        if (!newWorkingLayer.IsNull)
                            cmd.SetTarget(newWorkingLayer).SetWorkingLayer();
                    }
                    cmd.CommitToLatest();
                }
                else if (_dragMode == DragMode.EndHandle)
                {
                    var cmd = new CommandBuilder()
                        .SetProperty(_playbackEnd, _playbackEndAtDragStart, _playbackEnd.Value);
                    if (_currentFrame != null && _currentFrame.Value != _frameAtDragStart)
                    {
                        cmd.SetProperty(_currentFrame, _frameAtDragStart, _currentFrame.Value);
                        var newWorkingLayer = GetNewWorkingLayerAfterFrameChange(_frameAtDragStart, _currentFrame.Value);
                        if (!newWorkingLayer.IsNull)
                            cmd.SetTarget(newWorkingLayer).SetWorkingLayer();
                    }
                    cmd.CommitToLatest();
                }
                _dragMode = DragMode.None;
            }
        }
        else if (@event is InputEventMouseMotion motion)
        {
            _hoverFrame = XToFrame(motion.Position.X);
            QueueRedraw();
            if (_dragMode != DragMode.None)
            {
                switch (_dragMode)
                {
                    case DragMode.Frame:
                        _currentFrame.Value = FrameFromX(motion.Position.X);
                        break;
                    case DragMode.StartHandle:
                        int newStart = StartFromX(motion.Position.X);
                        _playbackStart.Value = newStart;
                        if (_currentFrame?.Value < newStart)
                            _currentFrame.Value = newStart;
                        break;
                    case DragMode.EndHandle:
                        int newEnd = EndFromX(motion.Position.X);
                        _playbackEnd.Value = newEnd;
                        if (_currentFrame?.Value >= newEnd)
                            _currentFrame.Value = newEnd - 1;
                        break;
                }
                AcceptEvent();
            }
        }
    }

    // ── Cursor ────────────────────────────────────────────────────────────────

    public override int _GetCursorShape(Vector2 atPosition)
    {
        if (HitStartHandle(atPosition) || HitEndHandle(atPosition))
            return (int)CursorShape.Hsize;
        if (_currentFrame != null && _playbackStart != null && _playbackEnd != null)
        {
            int frame = XToFrame(atPosition.X);
            if (frame >= _playbackStart.Value && frame < _playbackEnd.Value)
                return (int)CursorShape.PointingHand;
        }
        return (int)CursorShape.Arrow;
    }

    // ── Mutation helpers with constraints ────────────────────────────────

    /// <summary>
    /// Compute playhead frame from ruler-local X, clamped to [PlaybackStart, PlaybackEnd).
    /// </summary>
    private int FrameFromX(float localX)
    {
        int frame = XToFrame(localX);
        if (_playbackStart != null)
            frame = Mathf.Clamp(frame, _playbackStart.Value, _playbackEnd.Value - 1);
        return frame;
    }

    /// <summary>
    /// Compute new PlaybackStart from ruler-local X, clamped to [0, PlaybackEnd-1].
    /// </summary>
    private int StartFromX(float localX) =>
        Mathf.Min(XToFrame(localX), _playbackEnd.Value - 1);

    /// <summary>
    /// Compute new PlaybackEnd from ruler-local X, clamped to [PlaybackStart+1, ∞).
    /// </summary>
    private int EndFromX(float localX) =>
        Mathf.Max(XToFrame(localX), _playbackStart.Value + 1);

    // ── Working-layer switch on frame change ─────────────────────────────────

    /// <summary>Delegates to <see cref="SelectionManager.ComputeWorkingLayerForSwitchingFrame"/>.</summary>
    private Entity GetNewWorkingLayerAfterFrameChange(int oldFrame, int newFrame) =>
        _selectionManager?.ComputeWorkingLayerForSwitchingFrame(oldFrame, newFrame) ?? Entity.Null;
}