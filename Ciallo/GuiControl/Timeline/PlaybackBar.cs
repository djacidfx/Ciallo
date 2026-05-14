using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws a vertical line + triangular drag handle for a playback boundary (start or end).
/// Spans from the ruler's top edge down through the entire grid area.
/// Has <c>top_level = true</c> so it floats over other controls.
/// </summary>
[Tool, GlobalClass]
public partial class PlaybackBar : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public Color LineColor { get; set; } = new Color(0.25f, 0.85f, 0.25f, 0.9f);
    [Export] public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Width/height of the triangular handle drawn in the ruler band.
    /// Should match <see cref="TimelineRuler.HandleSize"/>.
    /// </summary>
    [Export] public float HandleSize { get; set; } = 10f;

    /// <summary>
    /// <c>true</c> → start bar (triangle body extends LEFT of the line).
    /// <c>false</c> → end bar (triangle body extends RIGHT of the line).
    /// </summary>
    [Export] public bool IsStart { get; set; } = true;

    // ── Private state ─────────────────────────────────────────────────────────
    private float _ppf = 20f;
    private float _scrollOffset;
    private int _frame;
    private Control _gridAnchor;
    private Control _rulerAnchor;

    // ── Bind ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire reactive sources.
    /// <paramref name="gridAnchor"/> defines the grid/track area; <paramref name="rulerAnchor"/>
    /// defines the ruler band that sits directly above the grid.
    /// </summary>
    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffsetFrame,
        ReactiveProperty<int> frame,
        Control gridAnchor,
        Control rulerAnchor)
    {
        _gridAnchor = gridAnchor;
        _rulerAnchor = rulerAnchor;

        pixelsPerFrame.CombineLatest(scrollOffsetFrame, (ppf, sof) => (ppf, sof * ppf))
            .Subscribe(t =>
            {
                _ppf = t.ppf;
                _scrollOffset = t.Item2;
                UpdateTransform();
            }).AddTo(this);
        frame.Subscribe(v =>
        {
            _frame = v;
            UpdateTransform();
        }).AddTo(this);

        gridAnchor.ItemRectChanged += UpdateTransform;
        rulerAnchor.ItemRectChanged += UpdateTransform;
    }

    // ── Transform ────────────────────────────────────────────────────────────

    private void UpdateTransform()
    {
        if (_gridAnchor == null || _rulerAnchor == null) return;

        // Frame 0 is at virtual x = 0, matching TimelineRuler's coordinate origin.
        float x = _gridAnchor.GlobalPosition.X + _frame * _ppf - _scrollOffset;

        // Bar is wide enough to contain HandleSize on one side + the line + a margin.
        float barWidth = HandleSize + LineWidth + 4f;
        float topY = _rulerAnchor.GlobalPosition.Y;
        float totalH = _rulerAnchor.Size.Y + _gridAnchor.Size.Y;

        GlobalPosition = new Vector2(x - barWidth * 0.5f, topY);
        Size = new Vector2(barWidth, totalH);
        QueueRedraw();
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        float cx = Size.X * 0.5f;

        // Vertical line spanning the full height (ruler + grid).
        DrawLine(new Vector2(cx, 0f), new Vector2(cx, Size.Y), LineColor, LineWidth);

        // Triangular handle in the ruler band (y ∈ [0, HandleSize]).
        if (IsStart)
            // Right edge at cx, body extends LEFT.
            DrawColoredPolygon(
                [new Vector2(cx - HandleSize, 0f), new Vector2(cx, 0f), new Vector2(cx, HandleSize)],
                LineColor);
        else
            // Left edge at cx, body extends RIGHT.
            DrawColoredPolygon(
                [new Vector2(cx, 0f), new Vector2(cx + HandleSize, 0f), new Vector2(cx, HandleSize)],
                LineColor);
    }
}