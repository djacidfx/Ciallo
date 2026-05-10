using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws frame-number ruler ticks and draggable playback-range handles.
/// </summary>
[Tool, GlobalClass]
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
    public Color OutOfRangeOverlay
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(0f, 0f, 0f, 0.22f);

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

    [Export] public Color TickColor
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

    public int LabelFontSize => GetThemeFontSize("font_size", "Label");
    public Color LabelColor => GetThemeColor("font_color", "Label");

    // ── Private state ────────────────────────────────────────────────────────
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;
    private float _fps = 24f;
    private ReactiveProperty<int> _currentFrame;
    private ReactiveProperty<int> _playbackStart;
    private ReactiveProperty<int> _playbackEnd;

    private enum DragMode { None, Frame, StartHandle, EndHandle }

    private DragMode _dragMode = DragMode.None;

    #region Setup

    /// <summary>Call once from TimelinePanel to wire zoom / scroll.</summary>
    public void Observe(ReactiveProperty<float> pixelsPerFrame, ReactiveProperty<float> scrollOffset,
        ReactiveProperty<float> fps)
    {
        pixelsPerFrame.Subscribe(v =>
        {
            _pixelsPerFrame = v;
            QueueRedraw();
        }).AddTo(this);
        scrollOffset.Subscribe(v =>
        {
            _scrollOffset = v;
            QueueRedraw();
        }).AddTo(this);
        fps.Subscribe(v =>
        {
            _fps = v;
            QueueRedraw();
        }).AddTo(this);
    }

    /// <summary>Wire the current-frame property (called from TimelinePanel.BindPlayhead).</summary>
    public void BindCurrentFrame(ReactiveProperty<int> currentFrame)
    {
        _currentFrame = currentFrame;
        currentFrame.Subscribe(_ => QueueRedraw()).AddTo(this);
    }

    /// <summary>Wire playback-range properties (called from TimelinePanel.BindTimeline).</summary>
    public void BindPlaybackRange(ReactiveProperty<int> playbackStart, ReactiveProperty<int> playbackEnd)
    {
        _playbackStart = playbackStart;
        _playbackEnd = playbackEnd;
        playbackStart.Subscribe(_ => QueueRedraw()).AddTo(this);
        playbackEnd.Subscribe(_ => QueueRedraw()).AddTo(this);
    }

    #endregion

    // ── Coordinate helpers ───────────────────────────────────────────────────

    /// <summary>Ruler-local pixel X of a frame's left edge. Frame 0 is the virtual origin.</summary>
    private float FrameToX(int frame) => frame * _pixelsPerFrame - _scrollOffset;

    /// <summary>Frame index whose left edge is at or before ruler-local X. May return 0 or negative.</summary>
    private int XToFrame(float x) => (int)((x + _scrollOffset) / _pixelsPerFrame);

    // ── Handle hit-tests ─────────────────────────────────────────────────────

    /// <summary>
    /// Start handle: right-angle triangle, right vertical edge at startX, body to the LEFT.
    /// Hit zone covers the triangle bounding box with a small horizontal tolerance.
    /// </summary>
    private bool HitStartHandle(Vector2 p)
    {
        if (_playbackStart == null) return false;
        float sx = FrameToX(_playbackStart.Value);
        return p.X >= sx - HandleSize - 2f && p.X <= sx + 2f && p.Y >= 0f && p.Y <= HandleSize + 4f;
    }

    /// <summary>
    /// End handle: right-angle triangle, left vertical edge at endX, body to the RIGHT.
    /// </summary>
    private bool HitEndHandle(Vector2 p)
    {
        if (_playbackEnd == null) return false;
        float ex = FrameToX(_playbackEnd.Value);
        return p.X >= ex - 2f && p.X <= ex + HandleSize + 2f && p.Y >= 0f && p.Y <= HandleSize + 4f;
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (_pixelsPerFrame <= 0f) return;

        float w = Size.X;
        float h = Size.Y;

        // ── Out-of-range overlay (full height) ──────────────────────────────
        if (_playbackStart != null && _playbackEnd != null)
        {
            float sx = FrameToX(_playbackStart.Value);
            float ex = FrameToX(_playbackEnd.Value);

            if (sx > 0f)
                DrawRect(new Rect2(0f, 0f, Mathf.Min(sx, w), h), OutOfRangeOverlay);

            float clampedEx = Mathf.Max(0f, ex);
            if (clampedEx < w)
                DrawRect(new Rect2(clampedEx, 0f, w - clampedEx, h), OutOfRangeOverlay);
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

                DrawLine(new Vector2(x, 0f), new Vector2(x, SecondsBandHeight), TickColor);
                DrawString(font, new Vector2(x + 2f, SecondsBandHeight - 2f),
                    $"{s}s", HorizontalAlignment.Left, -1, LabelFontSize, LabelColor);
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
            DrawLine(new Vector2(x, h - tickH), new Vector2(x, h), TickColor);

            // Frame 0 has no visible label — numbering goes …-2, -1, (silent 0), 1, 2…
            if (isMajor && showFrameLabels && frame != 0)
                DrawString(font, new Vector2(x + 2f, h - tickH - 2f),
                    frame.ToString(), HorizontalAlignment.Left, -1, LabelFontSize, LabelColor);
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
                    _dragMode = DragMode.StartHandle;
                else if (HitEndHandle(btn.Position))
                    _dragMode = DragMode.EndHandle;
                else if (_currentFrame != null)
                {
                    _dragMode = DragMode.Frame;
                    SetFrameFromX(btn.Position.X);
                }

                if (_dragMode != DragMode.None) AcceptEvent();
            }
            else
            {
                _dragMode = DragMode.None;
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragMode != DragMode.None)
        {
            switch (_dragMode)
            {
                case DragMode.Frame: SetFrameFromX(motion.Position.X); break;
                case DragMode.StartHandle: SetStartFromX(motion.Position.X); break;
                case DragMode.EndHandle: SetEndFromX(motion.Position.X); break;
            }
            AcceptEvent();
        }
    }

    // ── Mutation helpers with constraints ────────────────────────────────────

    /// <summary>
    /// Set playhead from ruler-local X, clamped to [PlaybackStart, PlaybackEnd).
    /// </summary>
    private void SetFrameFromX(float localX)
    {
        int frame = XToFrame(localX);
        if (_playbackStart != null)
            frame = Mathf.Clamp(frame, _playbackStart.Value, _playbackEnd.Value - 1);
        _currentFrame.Value = frame;
    }

    /// <summary>
    /// Drag PlaybackStart: clamped to [1, PlaybackEnd-1].
    /// Pushes playhead forward if it would fall below the new start.
    /// </summary>
    private void SetStartFromX(float localX)
    {
        int newStart = Mathf.Min(XToFrame(localX), _playbackEnd.Value - 1);
        _playbackStart.Value = newStart;
        if (_currentFrame != null && _currentFrame.Value < newStart)
            _currentFrame.Value = newStart;
    }

    /// <summary>
    /// Drag PlaybackEnd: clamped to [PlaybackStart+1, ∞).
    /// Pulls playhead back if it would land on or past the new end.
    /// </summary>
    private void SetEndFromX(float localX)
    {
        int newEnd = Mathf.Max(XToFrame(localX), _playbackStart.Value + 1);
        _playbackEnd.Value = newEnd;
        if (_currentFrame != null && _currentFrame.Value >= newEnd)
            _currentFrame.Value = newEnd - 1;
    }
}