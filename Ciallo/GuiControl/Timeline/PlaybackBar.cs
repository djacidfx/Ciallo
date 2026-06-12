using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws a vertical line + triangular drag handle for a playback boundary (start or end).
/// Spans from the ruler's top edge down through the entire grid area.
/// Has <c>top_level = true</c> so it floats over other controls.
/// </summary>
[Tool]
public partial class PlaybackBar : Control
{
    private const float EditorPreviewPixelsPerFrame = 32f;
    private const float EditorPreviewScrollOffsetFrame = -5f;
    private const int EditorPreviewPlaybackStart = 0;
    private const int EditorPreviewPlaybackEnd = 24;

    // ── Tunable exports ──────────────────────────────────────────────────────
    /// <summary>Color of the triangular handle and line segment within the ruler band (top).</summary>
    [Export]
    public Color HandleColor
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    }
    /// <summary>Color of the vertical line in the grid area (bottom).</summary>
    [Export]
    public Color LineColor
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    }
    [Export]
    public float LineWidth
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    } = 2f;

    /// <summary>Width/height of the triangular handle drawn in the ruler band.</summary>
    [Export]
    public float HandleSize
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    } = 10f;

    /// <summary>
    /// <c>true</c> → start bar (triangle body extends LEFT of the line).
    /// <c>false</c> → end bar (triangle body extends RIGHT of the line).
    /// </summary>
    [Export] public bool IsStart { get; set; } = true;

    [Export] public Control GridAnchor { get; set; }
    [Export] public Control RulerAnchor { get; set; }

    // Private state.
    private float _ppf = 20f;
    private float _scrollOffset;
    private int _frame;

    public override void _Ready()
    {
        if (GridAnchor != null)
            GridAnchor.ItemRectChanged += UpdateTransform;
        if (RulerAnchor != null)
            RulerAnchor.ItemRectChanged += UpdateTransform;

        UpdateTransform();
    }

    // Bind.

    /// <summary>Wire reactive sources.</summary>
    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffsetFrame,
        ReactiveProperty<int> frame)
    {
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
    }

    // Transform.

    private void UpdateTransform()
    {
        if (GridAnchor == null || RulerAnchor == null)
            return;

        if (Engine.IsEditorHint())
        {
            UpdateEditorPreviewTransform();
            return;
        }

        ApplyTransform(GridAnchor, RulerAnchor, _frame, _ppf, _scrollOffset);
    }

    private void UpdateEditorPreviewTransform()
    {
        if (GridAnchor == null || RulerAnchor == null) return;

        ApplyTransform(
            GridAnchor,
            RulerAnchor,
            IsStart ? EditorPreviewPlaybackStart : EditorPreviewPlaybackEnd,
            EditorPreviewPixelsPerFrame,
            EditorPreviewScrollOffsetFrame * EditorPreviewPixelsPerFrame);
    }

    private void ApplyTransform(Control gridAnchor, Control rulerAnchor, int frame, float pixelsPerFrame, float scrollOffset)
    {
        // Frame 0 is at virtual x = 0, matching TimelineRuler's coordinate origin.
        float x = gridAnchor.GlobalPosition.X + frame * pixelsPerFrame - scrollOffset;

        // Bar is wide enough to contain HandleSize on one side + the line + a margin.
        float barWidth = HandleSize + LineWidth + 4f;
        float topY = rulerAnchor.GlobalPosition.Y;
        float totalH = rulerAnchor.Size.Y + gridAnchor.Size.Y;

        GlobalPosition = new Vector2(x - barWidth * 0.5f, topY);
        Size = new Vector2(barWidth, totalH);
        QueueRedraw();
    }

    // Draw.

    public override void _Draw()
    {
        float cx = Size.X * 0.5f;
        float rulerH = RulerAnchor?.Size.Y ?? 0f;

        // Line in the ruler band (top).
        DrawLine(new Vector2(cx, 0f), new Vector2(cx, rulerH), HandleColor, LineWidth);
        // Line in the grid area (bottom).
        DrawLine(new Vector2(cx, rulerH), new Vector2(cx, Size.Y), LineColor, LineWidth);

        // Triangular handle in the ruler band (y ∈ [0, HandleSize]).
        if (IsStart)
            // Right edge at cx, body extends LEFT.
            DrawColoredPolygon(
                [new Vector2(cx - HandleSize, 0f), new Vector2(cx, 0f), new Vector2(cx, HandleSize)],
                HandleColor);
        else
            // Left edge at cx, body extends RIGHT.
            DrawColoredPolygon(
                [new Vector2(cx, 0f), new Vector2(cx + HandleSize, 0f), new Vector2(cx, HandleSize)],
                HandleColor);
    }
}
