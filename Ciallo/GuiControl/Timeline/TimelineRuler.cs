using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws frame-number ruler ticks and draggable playback-range handles.
/// <list type="bullet">
///   <item>Click / drag on the ruler body → moves the playhead (clamped to [start, end)).</item>
///   <item>Drag the <b>green start handle</b> (left-leaning triangle) → changes PlaybackStart.</item>
///   <item>Drag the <b>red end handle</b> (right-leaning triangle) → changes PlaybackEnd.</item>
/// </list>
/// All mutations enforce: 1 ≤ start &lt; end, playhead ∈ [start, end).
/// </summary>
[Tool, GlobalClass]
public partial class TimelineRuler : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public Color TickColor { get; set; } = new Color(0.65f, 0.65f, 0.65f);
    [Export] public Color LabelColor { get; set; } = new Color(0.9f, 0.9f, 0.9f);
    [Export] public Color PlayheadTickColor { get; set; } = new Color(1f, 0.65f, 0f);
    [Export] public Color PlaybackStartColor { get; set; } = new Color(0.25f, 0.85f, 0.25f, 0.9f);
    [Export] public Color PlaybackEndColor { get; set; } = new Color(0.9f, 0.25f, 0.25f, 0.9f);
    [Export] public Color OutOfRangeOverlay { get; set; } = new Color(0f, 0f, 0f, 0.22f);
    [Export] public int MajorTickHeight { get; set; } = 16;
    [Export] public int MinorTickHeight { get; set; } = 6;
    [Export] public int LabelFontSize { get; set; } = 11;
    /// <summary>Minimum pixel gap between displayed labels.</summary>
    [Export] public float MinLabelSpacingPx { get; set; } = 40f;
    /// <summary>Minimum pixel gap between tick marks.</summary>
    [Export] public float MinTickSpacingPx { get; set; } = 8f;
    /// <summary>Width and height of the triangular drag handle drawn at the ruler top.</summary>
    [Export] public float HandleSize { get; set; } = 10f;

    // ── Private state ────────────────────────────────────────────────────────
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;
    private ReactiveProperty<int> _currentFrame;
    private ReactiveProperty<int> _playbackStart;
    private ReactiveProperty<int> _playbackEnd;

    private enum DragMode { None, Frame, StartHandle, EndHandle }
    private DragMode _dragMode = DragMode.None;

    // ── Setup ────────────────────────────────────────────────────────────────

    /// <summary>Call once from TimelinePanel to wire zoom / scroll.</summary>
    public void Setup(ReactiveProperty<float> pixelsPerFrame, ReactiveProperty<float> scrollOffset)
    {
        pixelsPerFrame.Subscribe(v => { _pixelsPerFrame = v; QueueRedraw(); }).AddTo(this);
        scrollOffset.Subscribe(v => { _scrollOffset = v; QueueRedraw(); }).AddTo(this);
        Resized += QueueRedraw;
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

    // ── Coordinate helpers ───────────────────────────────────────────────────

    /// <summary>Ruler-local pixel X of a frame's left edge.</summary>
    private float FrameToX(int frame) => (frame - 1) * _pixelsPerFrame - _scrollOffset;

    /// <summary>Frame index whose left edge is at or before ruler-local X.</summary>
    private int XToFrame(float x) => Mathf.Max(1, (int)((x + _scrollOffset) / _pixelsPerFrame) + 1);

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

        // ── Out-of-range overlay ─────────────────────────────────────────────
        if (_playbackStart != null && _playbackEnd != null)
        {
            float sx = FrameToX(_playbackStart.Value);
            float ex = FrameToX(_playbackEnd.Value);

            // Overlay before start
            if (sx > 0f)
                DrawRect(new Rect2(0f, 0f, Mathf.Min(sx, w), h), OutOfRangeOverlay);

            // Overlay after end
            float clampedEx = Mathf.Max(0f, ex);
            if (clampedEx < w)
                DrawRect(new Rect2(clampedEx, 0f, w - clampedEx, h), OutOfRangeOverlay);
        }

        // ── Ticks & labels ───────────────────────────────────────────────────
        int tickStep = 1;
        while (_pixelsPerFrame * tickStep < MinTickSpacingPx) tickStep *= tickStep < 5 ? 5 : 2;

        int labelStep = tickStep;
        while (_pixelsPerFrame * labelStep < MinLabelSpacingPx) labelStep += tickStep;

        int startFrame = Mathf.Max(1, (int)(_scrollOffset / _pixelsPerFrame));
        int endFrame = (int)((_scrollOffset + w) / _pixelsPerFrame) + 3;

        var font = GetThemeDefaultFont();

        for (int frame = startFrame; frame <= endFrame; frame++)
        {
            bool isTickFrame = frame == 1 || frame % tickStep == 0;
            if (!isTickFrame) continue;

            float x = FrameToX(frame);
            if (x < -_pixelsPerFrame || x > w + _pixelsPerFrame) continue;

            bool isMajor = frame == 1 || frame % labelStep == 0;
            float tickH = isMajor ? MajorTickHeight : MinorTickHeight;
            DrawLine(new Vector2(x, h - tickH), new Vector2(x, h), TickColor);

            if (isMajor)
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

        // ── Playback-range lines and handles ─────────────────────────────────
        if (_playbackStart != null)
        {
            float sx = FrameToX(_playbackStart.Value);
            if (sx >= -HandleSize && sx <= w + HandleSize)
            {
                // Vertical boundary line
                DrawLine(new Vector2(sx, 0f), new Vector2(sx, h), PlaybackStartColor, 1.5f);

                // Left-leaning handle: right-angle triangle, right edge at sx, body extends LEFT
                //   (sx-W, 0) ──── (sx, 0)
                //                  |
                //              (sx, H)
                DrawColoredPolygon(
                    new[] { new Vector2(sx - HandleSize, 0f), new Vector2(sx, 0f), new Vector2(sx, HandleSize) },
                    PlaybackStartColor);
            }
        }

        if (_playbackEnd != null)
        {
            float ex = FrameToX(_playbackEnd.Value);
            if (ex >= -HandleSize && ex <= w + HandleSize)
            {
                // Vertical boundary line
                DrawLine(new Vector2(ex, 0f), new Vector2(ex, h), PlaybackEndColor, 1.5f);

                // Right-leaning handle: right-angle triangle, left edge at ex, body extends RIGHT
                //   (ex, 0) ──── (ex+W, 0)
                //   |
                //   (ex, H)
                DrawColoredPolygon(
                    new[] { new Vector2(ex, 0f), new Vector2(ex + HandleSize, 0f), new Vector2(ex, HandleSize) },
                    PlaybackEndColor);
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
        int newStart = Mathf.Clamp(XToFrame(localX), 1, _playbackEnd.Value - 1);
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
