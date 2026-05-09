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
    private float _dragStartOffset;
    private (float Start, float Range) _dragStartVirtualBounds; // virtual-space bounds at drag begin
    private float _dragStartX;
    private float _dragStartL;
    private float _dragStartR;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _ppf = DefaultPixelsPerFrame;
        Resized += QueueRedraw;
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
    /// The virtual space always covers [PlaybackStart, PlaybackEnd] plus whatever is
    /// currently visible, anchored so frame 0 is always included.
    /// </summary>
    private (float Start, float Range) ComputeVirtualBounds()
    {
        if (_setting == null) return (0f, MaxScrollFrames);

        float scrollOffsetFrame = _scrollOffset / _ppf;
        float startFrame = Mathf.Min(Mathf.Min(_playbackStartFrame, scrollOffsetFrame), 0f);
        float endFrame = Mathf.Max(Mathf.Max(_playbackEndFrame, scrollOffsetFrame + Size.X / _ppf), 0f);
        return (startFrame, endFrame - startFrame);
    }

    // ── Thumb geometry ───────────────────────────────────────────────────────

    private (float L, float R) GetDisplayThumb()
    {
        float w = Size.X;
        if (w <= 0f) return (0f, MinThumbWidth);

        var (virtualStart, range) = ComputeVirtualBounds();
        float virtualWidth = range * _ppf;
        float thumbW = Mathf.Clamp(w * w / virtualWidth, MinThumbWidth, w);
        // Thumb position is relative to the virtual-space left edge, not absolute scroll=0.
        float thumbL = (_scrollOffset - virtualStart * _ppf) / virtualWidth * w;
        thumbL = Mathf.Clamp(thumbL, 0f, w - thumbW);
        return (thumbL, thumbL + thumbW);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        var (l, r) = GetDisplayThumb();
        const float pad = 2f;

        DrawRect(new Rect2(0f, 0f, w, h), TrackColor);

        var thumbColor = _dragMode != DragMode.None ? ThumbActiveColor : ThumbColor;
        DrawRect(new Rect2(l, pad, r - l, h - pad * 2f), thumbColor);

        DrawRect(new Rect2(l, 0f, GrabZoneWidth, h), GrabHandleColor);
        DrawRect(new Rect2(r - GrabZoneWidth, 0f, GrabZoneWidth, h), GrabHandleColor);
    }

    // ── Input ────────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            if (btn.Pressed)
            {
                var (l, r) = GetDisplayThumb();
                float x = btn.Position.X;

                _dragStartOffset = _scrollOffset;
                _dragStartVirtualBounds = ComputeVirtualBounds();
                _dragStartX = x;
                _dragStartL = l;
                _dragStartR = r;

                if (x >= l && x < l + GrabZoneWidth)
                    _dragMode = DragMode.ZoomLeft;
                else if (x > r - GrabZoneWidth && x <= r)
                    _dragMode = DragMode.ZoomRight;
                else if (x >= l && x <= r)
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
            float dx = motion.Position.X - _dragStartX;
            float w = Size.X;

            switch (_dragMode)
            {
                case DragMode.Scroll:
                {
                    if (_setting != null)
                    {
                        _setting.ScrollOffsetPixels.Value = _dragStartOffset + dx;
                    }
                    else
                    {
                        _scrollOffset = _dragStartOffset + dx;
                        QueueRedraw();
                    }
                    break;
                }
                case DragMode.ZoomLeft:
                {
                    // Anchor: endFrame stays fixed; left handle moves freely (no boundary clamp).
                    if (_dragStartR <= 0f) break;
                    var (vs, range) = _dragStartVirtualBounds;
                    float newL = _dragStartL + dx; // unclamped — zoom continues past control edge
                    // frame = pixelPos * range / w + vs
                    float endFrame = _dragStartR * range / w + vs;
                    float newStartFrame = newL * range / w + vs;
                    float newSpan = endFrame - newStartFrame;
                    if (newSpan <= 0f) break;
                    float newPpf = Mathf.Clamp(w / newSpan, MinPixelsPerFrame, MaxPixelsPerFrame);
                    // Keep endFrame anchored: scrollOffset = endFrame * ppf - w
                    ApplyZoom(newPpf, endFrame * newPpf - w);
                    break;
                }
                case DragMode.ZoomRight:
                {
                    // Anchor: startFrame stays fixed; right handle moves freely.
                    if (_dragStartR <= 0f) break;
                    var (vs, range) = _dragStartVirtualBounds;
                    float newR = _dragStartR + dx; // unclamped
                    float startFrame = _dragStartL * range / w + vs;
                    float newEndFrame = newR * range / w + vs;
                    float newSpan = newEndFrame - startFrame;
                    if (newSpan <= 0f) break;
                    float newPpf = Mathf.Clamp(w / newSpan, MinPixelsPerFrame, MaxPixelsPerFrame);
                    // Keep startFrame anchored: scrollOffset = startFrame * ppf
                    ApplyZoom(newPpf, startFrame * newPpf);
                    break;
                }
            }

            AcceptEvent();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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