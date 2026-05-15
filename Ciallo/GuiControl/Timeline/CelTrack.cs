using System.Collections.Generic;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws the dope-sheet exposure track for one CelFolder in the right panel of its
/// <see cref="TrackRow"/> inside <see cref="TrackTree"/>.
/// <list type="bullet">
///   <item>Lives as a normal (non-TopLevel) child of the <see cref="TrackRow"/> HSplitContainer
///         and fills the right panel via <see cref="SizeFlags.ExpandFill"/>.</item>
///   <item>For every exposure key a boundary bar is drawn; consecutive bars are linked by
///         a line + arrowhead.</item>
/// </list>
/// Call <see cref="Observe"/> and <see cref="Bind"/> once after adding to the scene.
/// </summary>
[GlobalClass]
public partial class CelTrack : Control
{
    // ── Tunable ──────────────────────────────────────────────────────────────
    public float BarWidthRatio = 0.5f; // bar width = ppf * ratio
    public float MaxBarWidth = 16f;
    public float BarWidth => Mathf.Min(_ppf * BarWidthRatio, MaxBarWidth);
    public float ArrowHeadLength = 7f;
    public float ArrowHeadHalfWidth = 4f;
    public float LabelPad = 3f;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _ppf;
    private float _scrollOffset;
    private ObservableSortedList<int, Entity> _exposures;

    // ── Theme ─────────────────────────────────────────────────────────────────
    public Color BarColor;
    public Color LabelColor;
    public Color ArrowColor;
    public Font LabelFont;
    public int LabelFontSize;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CelTrack()
    {
        MouseFilter = MouseFilterEnum.Pass;
        ClipContents = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    // ── Theme init ────────────────────────────────────────────────────────────

    private void InitTheme()
    {
        var normalStyleBox = (StyleBoxFlat)GetThemeStylebox("normal", "Button");
        BarColor = normalStyleBox.BgColor;
        LabelColor = GetThemeColor("font_color", "Button");
        ArrowColor = LabelColor with { A = 0.4f };
        LabelFont = GetThemeFont("font", "Button");
        LabelFontSize = (int)(GetThemeFontSize("font_size", "Button") * 0.8f);
    }

    public override void _EnterTree() => InitTheme();

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged)
        {
            InitTheme();
            QueueRedraw();
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffsetFrame,
        CompositeDisposable subs)
    {
        pixelsPerFrame.CombineLatest(scrollOffsetFrame, (ppf, sof) => (ppf, sof * ppf))
            .Subscribe(t =>
            {
                _ppf = t.ppf;
                _scrollOffset = t.Item2;
                QueueRedraw();
            }).AddTo(subs);
    }

    public void Bind(ObservableSortedList<int, Entity> exposures, CompositeDisposable subs)
    {
        _exposures = exposures;
        exposures.ObserveChanged().Subscribe(_ => QueueRedraw()).AddTo(subs);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (_ppf <= 0f || _exposures == null) return;

        float h = Size.Y;
        float w = Size.X;
        float midY = h * 0.5f;
        float barW = BarWidth;

        var frames = new List<int>();
        foreach (var kv in _exposures)
            frames.Add(kv.Key);

        // ── Outer border ─────────────────────────────────────────────────────
        DrawRect(new Rect2(0f, 0f, w, h), Colors.Black, filled: false, width: 1f);

        for (int i = 0; i < frames.Count; i++)
        {
            int frame = frames[i];
            float x = frame * _ppf - _scrollOffset;

            // ── Boundary bar ─────────────────────────────────────────────────
            var barRect = new Rect2(x, 0f, barW, h);
            if (barRect.End.X > 0f && barRect.Position.X < w)
                DrawRect(barRect, BarColor);

            // ── Layer name label ──────────────────────────────────────────────
            var layerE = _exposures[frame];
            string name = layerE.Get<CommonLayerSetting>().Name.Value;
            float labelX = x + barW + LabelPad;
            float labelEnd = (i + 1 < frames.Count)
                ? frames[i + 1] * _ppf - _scrollOffset - ArrowHeadLength - LabelPad
                : w;
            float maxW = labelEnd - labelX;

            if (maxW > 0f && labelX < w)
                DrawString(LabelFont, new Vector2(labelX, midY + LabelFontSize * 0.35f),
                    name, HorizontalAlignment.Left, maxW, LabelFontSize, LabelColor);

            // ── Arrow to next bar ─────────────────────────────────────────────
            if (i + 1 >= frames.Count) continue;

            float nextX = frames[i + 1] * _ppf - _scrollOffset;
            float shaftStart = x + barW;
            float tipX = Mathf.Min(nextX, w + ArrowHeadLength); // allow tip to clip slightly

            if (tipX - shaftStart <= ArrowHeadLength) continue; // gap too narrow

            // Shaft line
            DrawLine(new(shaftStart, midY), new(tipX - ArrowHeadLength, midY), ArrowColor);

            // Arrowhead (triangle pointing right)
            Vector2 tip = new(tipX, midY);
            Vector2 p1 = new(tipX - ArrowHeadLength, midY - ArrowHeadHalfWidth);
            Vector2 p2 = new(tipX - ArrowHeadLength, midY + ArrowHeadHalfWidth);
            DrawColoredPolygon([tip, p1, p2], ArrowColor);
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event) { }

    public override int _GetCursorShape(Vector2 atPosition)
    {
        if (_ppf <= 0f || _exposures == null)
            return (int)CursorShape.Arrow;

        float barW = BarWidth;
        foreach (var kv in _exposures)
        {
            float x = kv.Key * _ppf - _scrollOffset;
            if (atPosition.X >= x && atPosition.X < x + barW)
                return (int)CursorShape.PointingHand;
        }

        return (int)CursorShape.Arrow;
    }
}