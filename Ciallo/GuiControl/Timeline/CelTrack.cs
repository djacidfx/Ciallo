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
    private FolderLayerSetting _folderSetting;
    private TrackHeaderBlock _headerBlock;
    private Control _anchor; // BackgroundGrid – defines x-origin and width
    private ScrollContainer _vscroll; // TrackVScroll – fires Scrolled when tree scrolls

    // ── Theme ─────────────────────────────────────────────────────────────────
    private Color _barColor;
    private Color _labelColor;
    private Color _arrowColor;
    private Font _labelFont;
    private int _labelFontSize;

    // ── Delegates stored for unsubscription ───────────────────────────────────
    private NotifyCollectionChangedEventHandler<KeyValuePair<int, Entity>> _onExposuresChanged;
    private Range.ValueChangedEventHandler _onVScrollChanged;

    // ── Constructor ───────────────────────────────────────────────────────────

    public CelTrack()
    {
        TopLevel = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    // ── Theme init ────────────────────────────────────────────────────────────

    private void InitTheme()
    {
        _barColor = GetThemeColor("font_color", "Label") with { A = 0.65f };
        _labelColor = GetThemeColor("font_color", "Label");
        _arrowColor = _labelColor with { A = 0.4f };
        _labelFont = GetThemeFont("font", "Label");
        _labelFontSize = (int)(GetThemeFontSize("font_size", "Label") * 0.8f);
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

    // ── Observe ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire this track to its data sources.  Must be called once after the node is added to the scene.
    /// </summary>
    public void Observe(
        TimelineSetting setting,
        Entity celFolderE,
        TrackHeaderBlock headerBlock,
        Control anchor,
        ScrollContainer vscroll)
    {
        _folderSetting = celFolderE.Get<FolderLayerSetting>();
        _headerBlock = headerBlock;
        _anchor = anchor;
        _vscroll = vscroll;

        // Zoom / scroll
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

        // Exposure changes
        _onExposuresChanged = OnExposuresChanged;
        _folderSetting.Exposures.CollectionChanged += _onExposuresChanged;

        // Position / size sync
        _anchor.ItemRectChanged += UpdateLayout;
        _headerBlock.ItemRectChanged += UpdateLayout;
        _headerBlock.VisibilityChanged += UpdateLayout;
        _onVScrollChanged = _ => UpdateLayout();
        _vscroll.GetVScrollBar().ValueChanged += _onVScrollChanged;

        UpdateLayout();
    }

    private void OnExposuresChanged(in NotifyCollectionChangedEventArgs<KeyValuePair<int, Entity>> _) => QueueRedraw();

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
        _folderSetting?.Exposures?.CollectionChanged -= _onExposuresChanged;
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
        if (_ppf <= 0f || _folderSetting?.Exposures == null) return;

        float h = Size.Y;
        float w = Size.X;
        float midY = h * 0.5f;
        float barW = _ppf * BarWidthRatio;

        var frames = new List<int>();
        foreach (var kv in _folderSetting.Exposures)
            frames.Add(kv.Key);
        frames.Sort();

        // ── Outer border ─────────────────────────────────────────────────────
        DrawRect(new Rect2(0f, 0f, w, h), Colors.Black, filled: false, width: 10f);

        for (int i = 0; i < frames.Count; i++)
        {
            int frame = frames[i];
            float x = frame * _ppf - _scrollOffset;

            // ── Boundary bar ─────────────────────────────────────────────────
            var barRect = new Rect2(x, 0f, barW, h);
            if (barRect.End.X > 0f && barRect.Position.X < w)
                DrawRect(barRect, _barColor);

            // ── Layer name label ──────────────────────────────────────────────
            var layerE = _folderSetting.Exposures[frame];
            if (!layerE.IsNull && layerE.Has<CommonLayerSetting>())
            {
                string name = layerE.Get<CommonLayerSetting>().Name.Value;
                float labelX = x + barW + LabelPad;
                float labelEnd = (i + 1 < frames.Count)
                    ? frames[i + 1] * _ppf - _scrollOffset - ArrowHeadLength - LabelPad
                    : w;
                float maxW = labelEnd - labelX;

                if (maxW > 0f && labelX < w)
                    DrawString(_labelFont, new Vector2(labelX, midY + _labelFontSize * 0.35f),
                        name, HorizontalAlignment.Left, maxW, _labelFontSize, _labelColor);
            }

            // ── Arrow to next bar ─────────────────────────────────────────────
            if (i + 1 >= frames.Count) continue;

            float nextX = frames[i + 1] * _ppf - _scrollOffset;
            float shaftStart = x + barW;
            float tipX = Mathf.Min(nextX, w + ArrowHeadLength); // allow tip to clip slightly

            if (tipX - shaftStart <= ArrowHeadLength) continue; // gap too narrow

            // Shaft line
            DrawLine(
                new Vector2(shaftStart, midY),
                new Vector2(tipX - ArrowHeadLength, midY),
                _arrowColor);

            // Arrowhead (triangle pointing right)
            Vector2 tip = new(tipX, midY);
            Vector2 p1 = new(tipX - ArrowHeadLength, midY - ArrowHeadHalfWidth);
            Vector2 p2 = new(tipX - ArrowHeadLength, midY + ArrowHeadHalfWidth);
            DrawColoredPolygon([tip, p1, p2], _arrowColor);
        }
    }

    // ── Cursor ────────────────────────────────────────────────────────────────

    public override int _GetCursorShape(Vector2 atPosition)
    {
        if (_ppf <= 0f || _folderSetting?.Exposures == null)
            return (int)CursorShape.Arrow;

        float barW = _ppf * BarWidthRatio;
        foreach (var kv in _folderSetting.Exposures)
        {
            float x = kv.Key * _ppf - _scrollOffset;
            if (atPosition.X >= x && atPosition.X < x + barW)
                return (int)CursorShape.Hsize;
        }

        return (int)CursorShape.Arrow;
    }
}