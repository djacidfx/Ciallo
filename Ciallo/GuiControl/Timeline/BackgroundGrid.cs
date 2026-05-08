using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws the background column grid in the track area.
/// TODO: implement row-based grid once track rows are defined.
/// </summary>
[Tool, GlobalClass]
public partial class BackgroundGrid : Control
{
    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export] public Color ColumnLineColor { get; set; } = new Color(1f, 1f, 1f, 0.06f);
    [Export] public Color MajorColumnLineColor { get; set; } = new Color(1f, 1f, 1f, 0.14f);
    /// <summary>Draw a major line every N minor-tick frames.</summary>
    [Export] public int MajorColumnInterval { get; set; } = 5;
    /// <summary>Minimum pixel gap between drawn column lines.</summary>
    [Export] public float MinColumnSpacingPx { get; set; } = 6f;

    // ── Private state ────────────────────────────────────────────────────────
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;

    // ── Setup ────────────────────────────────────────────────────────────────

    public void Setup(ReactiveProperty<float> pixelsPerFrame, ReactiveProperty<float> scrollOffset)
    {
        pixelsPerFrame.Subscribe(v => { _pixelsPerFrame = v; QueueRedraw(); }).AddTo(this);
        scrollOffset.Subscribe(v => { _scrollOffset = v; QueueRedraw(); }).AddTo(this);
        Resized += QueueRedraw;
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (_pixelsPerFrame <= 0f) return;

        float w = Size.X;
        float h = Size.Y;

        // Choose step so columns are at least MinColumnSpacingPx apart
        int step = 1;
        while (_pixelsPerFrame * step < MinColumnSpacingPx) step *= step < 5 ? 5 : 2;

        int startFrame = Mathf.Max(1, (int)(_scrollOffset / _pixelsPerFrame));
        int endFrame = (int)((_scrollOffset + w) / _pixelsPerFrame) + 2;

        for (int frame = startFrame; frame <= endFrame; frame++)
        {
            if (frame != 1 && frame % step != 0) continue;

            float x = (frame - 1) * _pixelsPerFrame - _scrollOffset;
            if (x < -_pixelsPerFrame || x > w + _pixelsPerFrame) continue;

            bool isMajor = frame == 1 || frame % (step * MajorColumnInterval) == 0;
            DrawLine(new Vector2(x, 0f), new Vector2(x, h),
                isMajor ? MajorColumnLineColor : ColumnLineColor);
        }
    }
}
