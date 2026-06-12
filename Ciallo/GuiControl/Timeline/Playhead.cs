using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Playhead has top_level = true so it floats over other controls.
/// Position and size are updated reactively from PixelsPerFrame / ScrollOffset / CurrentFrame,
/// with an optional center-anchored visual preview while the ruler is being dragged.
/// </summary>
[Tool]
public partial class Playhead : Control
{
    private const float EditorPreviewPixelsPerFrame = 32f;
    private const float EditorPreviewScrollOffsetFrame = -5f;
    private const int EditorPreviewCurrentFrame = 1;

    // ── Tunable exports ──────────────────────────────────────────────────────
    [Export]
    public Color BorderColor
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    }
    [Export]
    public float BorderWidth
    {
        get; set
        {
            field = value;
            QueueRedraw();
        }
    } = 1.5f;

    [Export] public Control GridAnchor { get; set; }
    [Export] public Control RulerAnchor { get; set; }

    // Private state.
    private float _pixelsPerFrame = 20f;
    private float _scrollOffset;
    private int _currentFrame = 1;
    private float? _previewCenterRulerX;

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
        ReactiveProperty<int> currentFrame)
    {
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
    }

    // Transform.

    /// <summary>
    /// Move the playhead visually so its center sits at the given ruler-local X without changing CurrentFrame.
    /// </summary>
    public void PreviewAtRulerCenterX(float centerX)
    {
        _previewCenterRulerX = centerX;
        UpdateTransform();
    }

    /// <summary>Return the playhead to its CurrentFrame-driven position.</summary>
    public void ClearPreview()
    {
        _previewCenterRulerX = null;
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (GridAnchor == null || RulerAnchor == null)
            return;

        if (Engine.IsEditorHint())
        {
            UpdateEditorPreviewTransform();
            return;
        }

        ApplyTransform(GridAnchor, RulerAnchor, _currentFrame, _pixelsPerFrame, _scrollOffset, _previewCenterRulerX);
    }

    private void UpdateEditorPreviewTransform()
    {
        if (GridAnchor == null || RulerAnchor == null) return;

        ApplyTransform(
            GridAnchor,
            RulerAnchor,
            EditorPreviewCurrentFrame,
            EditorPreviewPixelsPerFrame,
            EditorPreviewScrollOffsetFrame * EditorPreviewPixelsPerFrame);
    }

    private void ApplyTransform(
        Control gridAnchor,
        Control rulerAnchor,
        int frame,
        float pixelsPerFrame,
        float scrollOffset,
        float? previewCenterRulerX = null)
    {
        // Frame 0 is at virtual x = 0, matching TimelineRuler's coordinate origin.
        float localX = previewCenterRulerX.HasValue
            ? previewCenterRulerX.Value - pixelsPerFrame * 0.5f
            : frame * pixelsPerFrame - scrollOffset;
        float x = gridAnchor.GlobalPosition.X + localX;
        float topY = rulerAnchor.GlobalPosition.Y;
        float totalH = rulerAnchor.Size.Y + gridAnchor.Size.Y;

        GlobalPosition = new Vector2(x, topY);
        Size = new Vector2(pixelsPerFrame, totalH);
        QueueRedraw();
    }

    // Draw.

    public override void _Draw()
    {
        DrawLine(new Vector2(0f, 0f), new Vector2(0f, Size.Y), BorderColor, BorderWidth);
        DrawLine(new Vector2(Size.X, 0f), new Vector2(Size.X, Size.Y), BorderColor, BorderWidth);
    }
}
