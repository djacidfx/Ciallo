using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Block-shaped playhead that covers one full frame column.
/// Has top_level = true so it floats over other controls.
/// Position and size are updated reactively from PixelsPerFrame / ScrollOffset / CurrentFrame.
/// </summary>
[Tool, GlobalClass]
public partial class Playhead : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public Color FillColor { get; set; } = new Color(1f, 0.65f, 0f, 0.35f);
    [Export] public Color BorderColor { get; set; } = new Color(1f, 0.65f, 0f, 0.85f);
    [Export] public float BorderWidth { get; set; } = 1.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;
    private int _currentFrame = 1;
    private Control _anchor; // BackgroundGrid — defines the painted area bounds

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire reactive sources. <paramref name="anchor"/> is the control whose rect the playhead spans.
    /// </summary>
    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffsetFrame,
        ReactiveProperty<int> currentFrame,
        Control anchor)
    {
        _anchor = anchor;

        pixelsPerFrame.CombineLatest(scrollOffsetFrame, (ppf, sof) => (ppf, sof * ppf))
            .Subscribe(t =>
            {
                _pixelsPerFrame = t.ppf;
                _scrollOffset = t.Item2;
                UpdateTransform();
            }).AddTo(this);
        currentFrame.Subscribe(v =>
        {
            _currentFrame = v;
            UpdateTransform();
        }).AddTo(this);

        anchor.ItemRectChanged += UpdateTransform; // covers resize + global move
    }

    // ── Transform ────────────────────────────────────────────────────────────

    private void UpdateTransform()
    {
        if (_anchor == null) return;

        // Frame 0 is at virtual x = 0, matching TimelineRuler's coordinate origin.
        float x = _anchor.GlobalPosition.X + _currentFrame * _pixelsPerFrame - _scrollOffset;

        GlobalPosition = new Vector2(x, _anchor.GlobalPosition.Y);
        Size = new Vector2(_pixelsPerFrame, _anchor.Size.Y);
        QueueRedraw();
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, FillColor);
        DrawRect(rect, BorderColor, false, BorderWidth);
    }
}