using System.Collections.Generic;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Draws the dope-sheet exposure track for one CelFolder in the Timeline's TrackArea.
/// <list type="bullet">
///   <item>Has <c>TopLevel = true</c> — floats over the BackgroundGrid, positioned to match its <see cref="TrackHeaderBlock"/>.</item>
///   <item>For every exposure key a boundary bar is drawn; consecutive bars are linked by a line + arrowhead.</item>
/// </list>
/// Call <see cref="Observe"/> once after adding to the scene.
/// </summary>
[GlobalClass]
public partial class CelTrack : Control
{
    // ── Tunable ──────────────────────────────────────────────────────────────
    private const float BarWidthRatio = 0.5f; // bar width = ppf * ratio
    private const float ArrowHeadLength = 7f;
    private const float ArrowHeadHalfWidth = 4f;
    private const float LabelPad = 3f;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _ppf;
    private float _scrollOffset;
    private ObservableDictionary<int, Entity> _exposures;
    private TrackHeaderBlock _headerBlock;
    private Control _anchor; // BackgroundGrid – defines x-origin and width
    private ScrollContainer _vscroll; // TrackVScroll – fires Scrolled when tree scrolls

    // ── Theme ─────────────────────────────────────────────────────────────────
    public Color BarColor;
    public Color LabelColor;
    public Color ArrowColor;
    public Font LabelFont;
    public int LabelFontSize;

    // ── Delegates stored for unsubscription ───────────────────────────────────
    private Range.ValueChangedEventHandler _onVScrollChanged;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CelTrack()
    {
        TopLevel = true;
        MouseFilter = MouseFilterEnum.Pass;
    }

    // ── Theme init ────────────────────────────────────────────────────────────

    private void InitTheme()
    {
        BarColor = GetThemeColor("font_color", "Label") with { A = 0.65f };
        LabelColor = GetThemeColor("font_color", "Label");
        ArrowColor = LabelColor with { A = 0.4f };
        LabelFont = GetThemeFont("font", "Label");
        LabelFontSize = (int)(GetThemeFontSize("font_size", "Label") * 0.8f);
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

    public void Bind(ObservableDictionary<int, Entity> exposures, CompositeDisposable subs)
    {
        // Two-way binding on Exposures:
        _exposures = exposures;
        exposures.ObserveChanged().Subscribe(_ => QueueRedraw()).AddTo(subs);
    }

    // ── Layout sync ───────────────────────────────────────────────────────────

    private void UpdateLayout()
    {
        Visible = _headerBlock.IsVisibleInTree();
        if (!Visible) return;

        var anchorRect = _anchor.GetGlobalRect();
        var headerRect = _headerBlock.GetGlobalRect();
        GlobalPosition = new Vector2(anchorRect.Position.X, headerRect.Position.Y);
        Size = new Vector2(anchorRect.Size.X, headerRect.Size.Y);
        QueueRedraw();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    public override void _ExitTree()
    {
        _anchor?.ItemRectChanged -= UpdateLayout;
        if (_headerBlock != null)
        {
            _headerBlock.ItemRectChanged -= UpdateLayout;
            _headerBlock.VisibilityChanged -= UpdateLayout;
        }
        _vscroll?.GetVScrollBar()?.ValueChanged -= _onVScrollChanged;
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        if (_ppf <= 0f || _exposures == null) return;

        float h = Size.Y;
        float w = Size.X;
        float midY = h * 0.5f;
        float barW = _ppf * BarWidthRatio;

        var frames = new List<int>();
        foreach (var kv in _exposures)
            frames.Add(kv.Key);
        frames.Sort();

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

    // ── Cursor ────────────────────────────────────────────────────────────────

    public override int _GetCursorShape(Vector2 atPosition)
    {
        if (_ppf <= 0f || _exposures == null)
            return (int)CursorShape.Arrow;

        float barW = _ppf * BarWidthRatio;
        foreach (var kv in _exposures)
        {
            float x = kv.Key * _ppf - _scrollOffset;
            if (atPosition.X >= x && atPosition.X < x + barW)
                return (int)CursorShape.PointingHand;
        }

        return (int)CursorShape.Arrow;
    }
}