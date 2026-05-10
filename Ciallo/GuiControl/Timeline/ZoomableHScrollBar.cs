using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// A zoomable horizontal scrollbar.
/// <list type="bullet">
///   <item>Drag the <b>thumb body</b> to scroll.</item>
///   <item>Drag the <b>left grab handle</b> to zoom in / out while anchoring the right edge.</item>
///   <item>Drag the <b>right grab handle</b> to zoom in / out while anchoring the left edge.</item>
/// </list>
/// Call <see cref="Setup"/> after adding to the tree to wire reactive state from <see cref="TimelineSetting"/>.
/// </summary>
[Tool, GlobalClass]
public partial class ZoomableHScrollBar : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public float MinPixelsPerFrame { get; set; } = 4f;
    [Export] public float MaxPixelsPerFrame { get; set; } = 128f;
    [Export] public float DefaultPixelsPerFrame { get; set; } = 20f;

    /// <summary>
    /// Static fallback total frame span used in editor / before <see cref="Setup"/> is called.
    /// At runtime this value acts as a minimum — the actual span is derived from PlaybackEnd.
    /// </summary>
    [Export] public float MaxScrollFrames { get; set; } = 512f;

    /// <summary>Enforced minimum thumb pixel width so the bar stays grabbable.</summary>
    [Export] public float MinThumbWidth { get; set; } = 24f;

    /// <summary>Width of each end grab zone in pixels.</summary>
    [Export] public float GrabZoneWidth { get; set; } = 8f;

    [Export(PropertyHint.Range, "0, 1.0, 0.01")]
    public float ScrollZoneWidthRatio
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 1.0f;

    // ── Colors ───────────────────────────────────────────────────────────────
    [Export] public Color TrackColor { get; set; } = new Color(0.12f, 0.12f, 0.12f);
    [Export] public Color ThumbColor { get; set; } = new Color(0.38f, 0.38f, 0.38f);
    [Export] public Color ThumbActiveColor { get; set; } = new Color(0.52f, 0.52f, 0.52f);
    [Export] public Color GrabHandleColor { get; set; } = new Color(0.62f, 0.62f, 0.62f);

    // ── Reactive state (owned by TimelineSetting) ────────────────────────────
    private TimelineSetting _setting;

    // Local cache — updated by subscriptions, also used by [Tool] editor preview
    private float _ppf;
    private float _scrollOffset;
    private int _playbackStartFrame = 1;
    private int _playbackEndFrame = 25;

    // ── Drag state ───────────────────────────────────────────────────────────
    private enum DragMode { None, Scroll, ZoomLeft, ZoomRight }

    private DragMode _dragMode = DragMode.None;
    private float _dragScrollOffset; // scroll offset (pixels) frozen at drag start
    private (float Start, float Range) _dragVirtualBounds; // virtual timeline bounds (frames) frozen at drag start
    private float _dragMouseX; // mouse X (screen pixels) at drag start
    private float _dragThumbLeft; // thumb left edge (track pixels) at drag start
    private float _dragThumbRight; // thumb right edge (track pixels) at drag start

    /// <summary>Effective pixel width of the scroll track = Size.X * ScrollZoneWidthRatio.</summary>
    private float ScrollZoneWidth => Size.X * ScrollZoneWidthRatio;

    /// <summary>Left pixel offset so the scroll track is horizontally centered.</summary>
    private float ScrollZoneOffset => (Size.X - ScrollZoneWidth) / 2f;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _ppf = DefaultPixelsPerFrame;
    }

    /// <summary>
    /// Wire reactive zoom/scroll state from the document's <see cref="TimelineSetting"/>.
    /// Must be called once after the node is in the tree.
    /// </summary>
    public void Setup(TimelineSetting setting)
    {
        _setting = setting;

        _ppf = setting.PixelsPerFrame.Value;
        _scrollOffset = setting.ScrollOffsetPixels.Value;
        _playbackStartFrame = setting.PlaybackStart.Value;
        _playbackEndFrame = setting.PlaybackEnd.Value;

        setting.PixelsPerFrame.Subscribe(v =>
        {
            _ppf = v;
            QueueRedraw();
        }).AddTo(this);
        setting.ScrollOffsetPixels.Subscribe(v =>
        {
            _scrollOffset = v;
            QueueRedraw();
        }).AddTo(this);
        setting.PlaybackStart.Subscribe(v =>
        {
            _playbackStartFrame = v;
            QueueRedraw();
        }).AddTo(this);
        setting.PlaybackEnd.Subscribe(v =>
        {
            _playbackEndFrame = v;
            QueueRedraw();
        }).AddTo(this);
    }

    // ── Virtual frame span ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the virtual timeline bounds: the leftmost frame and total frame span.
    /// The virtual space always covers [PlaybackStart, PlaybackEnd] plus(extent to) whatever is currently visible 
    /// </summary>
    private (float Start, float Range) ComputeVirtualBounds()
    {
        if (_setting == null) return (0f, MaxScrollFrames);

        float scrollOffsetFrame = _scrollOffset / _ppf;
        float startFrame = Mathf.Min(_playbackStartFrame, scrollOffsetFrame);
        float endFrame = Mathf.Max(_playbackEndFrame, scrollOffsetFrame + Size.X / _ppf);
        return (startFrame, endFrame - startFrame);
    }

    // ── Thumb geometry ───────────────────────────────────────────────────────

    private (float L, float R) GetDisplayThumb()
    {
        float trackPx = ScrollZoneWidth;
        if (trackPx <= 0f) return (0f, MinThumbWidth);

        var (virtualStartFrame, virtualRangeFrames) = ComputeVirtualBounds();
        float virtualRangePx = virtualRangeFrames * _ppf;
        float thumbW = Mathf.Clamp(trackPx * Size.X / virtualRangePx, MinThumbWidth, trackPx);
        // Thumb position is relative to the virtual-space left edge, not absolute scroll=0.
        float thumbLeft = (_scrollOffset - virtualStartFrame * _ppf) / virtualRangePx * trackPx;
        thumbLeft = Mathf.Clamp(thumbLeft, 0f, trackPx - thumbW);
        return (thumbLeft, thumbLeft + thumbW);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float h = Size.Y;
        var (thumbLeft, thumbRight) = GetDisplayThumb();
        float trackOffset = ScrollZoneOffset;
        const float pad = 2f;

        DrawRect(new Rect2(trackOffset, 0f, ScrollZoneWidth, h), TrackColor);

        var thumbColor = _dragMode != DragMode.None ? ThumbActiveColor : ThumbColor;
        DrawRect(new Rect2(thumbLeft + trackOffset, pad, thumbRight - thumbLeft, h - pad * 2f), thumbColor);

        DrawRect(new Rect2(thumbLeft + trackOffset, 0f, GrabZoneWidth, h), GrabHandleColor);
        DrawRect(new Rect2(thumbRight - GrabZoneWidth + trackOffset, 0f, GrabZoneWidth, h), GrabHandleColor);
    }

    // ── Input ────────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            if (btn.Pressed)
            {
                var (thumbLeft, thumbRight) = GetDisplayThumb();
                float trackLocalX = btn.Position.X - ScrollZoneOffset;

                _dragScrollOffset = _scrollOffset;
                _dragVirtualBounds = ComputeVirtualBounds();
                _dragMouseX = btn.Position.X;
                _dragThumbLeft = thumbLeft;
                _dragThumbRight = thumbRight;

                if (trackLocalX >= thumbLeft && trackLocalX < thumbLeft + GrabZoneWidth)
                    _dragMode = DragMode.ZoomLeft;
                else if (trackLocalX > thumbRight - GrabZoneWidth && trackLocalX <= thumbRight)
                    _dragMode = DragMode.ZoomRight;
                else if (trackLocalX >= thumbLeft && trackLocalX <= thumbRight)
                    _dragMode = DragMode.Scroll;
                else
                    _dragMode = DragMode.None;

                if (_dragMode != DragMode.None) AcceptEvent();
            }
            else
            {
                _dragMode = DragMode.None;
                QueueRedraw();
                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragMode != DragMode.None)
        {
            float mouseDx = motion.Position.X - _dragMouseX;
            float trackPx = ScrollZoneWidth;
            float virtualRangePx = _dragVirtualBounds.Range * _ppf;

            var (virtualStartFrame, virtualRangeFrames) = _dragVirtualBounds;
            float TrackPxToFrame(float trackPos) => trackPos * virtualRangeFrames / trackPx + virtualStartFrame;

            switch (_dragMode)
            {
                case DragMode.Scroll:
                {
                    float newScrollOffset = _dragScrollOffset + mouseDx * virtualRangePx / trackPx;
                    if (_setting != null)
                        _setting.ScrollOffsetPixels.Value = newScrollOffset;
                    else
                    {
                        _scrollOffset = newScrollOffset;
                        QueueRedraw();
                    }
                    break;
                }
                case DragMode.ZoomLeft:
                {
                    if (trackPx <= 0f) break;
                    float anchoredRightFrame = TrackPxToFrame(_dragThumbRight);
                    float newThumbLeft = _dragThumbLeft + mouseDx;
                    float newViewportLeft = TrackPxToFrame(newThumbLeft);
                    float newVisibleFrames = anchoredRightFrame - newViewportLeft;
                    if (newVisibleFrames <= 0f) break;
                    float newPpf = Mathf.Clamp(Size.X / newVisibleFrames, MinPixelsPerFrame, MaxPixelsPerFrame);
                    ApplyZoom(newPpf, anchoredRightFrame * newPpf - Size.X);
                    break;
                }
                case DragMode.ZoomRight:
                {
                    if (trackPx <= 0f) break;
                    float anchoredLeftFrame = TrackPxToFrame(_dragThumbLeft);
                    float newThumbRight = _dragThumbRight + mouseDx;
                    float newViewportRight = TrackPxToFrame(newThumbRight);
                    float newVisibleFrames = newViewportRight - anchoredLeftFrame;
                    if (newVisibleFrames <= 0f) break;
                    float newPpf = Mathf.Clamp(Size.X / newVisibleFrames, MinPixelsPerFrame, MaxPixelsPerFrame);
                    ApplyZoom(newPpf, anchoredLeftFrame * newPpf);
                    break;
                }
            }

            AcceptEvent();
        }
    }

    private void ApplyZoom(float ppf, float scrollOffset)
    {
        if (_setting != null)
        {
            _setting.PixelsPerFrame.Value = ppf;
            _setting.ScrollOffsetPixels.Value = scrollOffset;
        }
        else
        {
            _ppf = ppf;
            _scrollOffset = scrollOffset;
            QueueRedraw();
        }
    }
}