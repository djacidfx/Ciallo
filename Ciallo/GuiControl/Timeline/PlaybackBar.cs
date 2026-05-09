using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws a thin vertical line marking a playback boundary (start or end).
/// Position is updated reactively.  Has <c>top_level = true</c> so it floats over other controls.
/// </summary>
[Tool, GlobalClass]
public partial class PlaybackBar : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public Color LineColor { get; set; } = new Color(0.25f, 0.85f, 0.25f, 0.9f);
    [Export] public float LineWidth { get; set; } = 2f;

    // ── Private state ─────────────────────────────────────────────────────────
    private float _ppf = 20f;
    private float _scrollOffset;
    private int _frame = 1;
    private Control _anchor;

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire reactive sources.  The bar is drawn at the left edge of <paramref name="frame"/>
    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffset,
        ReactiveProperty<int> frame,
        Control anchor)
    {
        _anchor = anchor;

        pixelsPerFrame.Subscribe(v =>
        {
            _ppf = v;
            UpdateTransform();
        }).AddTo(this);
        scrollOffset.Subscribe(v =>
        {
            _scrollOffset = v;
            UpdateTransform();
        }).AddTo(this);
        frame.Subscribe(v =>
        {
            _frame = v;
            UpdateTransform();
        }).AddTo(this);
        anchor.ItemRectChanged += UpdateTransform;
    }

    // ── Transform ────────────────────────────────────────────────────────────

    private void UpdateTransform()
    {
        if (_anchor == null) return;

        // Left edge of _frame: (frame - 1) * ppf - scrollOffset relative to anchor
        float x = _anchor.GlobalPosition.X + (_frame - 1) * _ppf - _scrollOffset;

        // Center the control on the line so the drawn line sits exactly at the boundary
        GlobalPosition = new Vector2(x - Size.X * 0.5f, _anchor.GlobalPosition.Y);
        Size = new Vector2(LineWidth + 4f, _anchor.Size.Y);
        QueueRedraw();
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float cx = Size.X * 0.5f;
        DrawLine(new Vector2(cx, 0f), new Vector2(cx, Size.Y), LineColor, LineWidth);
    }
}