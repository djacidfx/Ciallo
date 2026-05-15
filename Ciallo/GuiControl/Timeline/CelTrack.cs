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
    private int _playbackStart;
    private int _playbackEnd;
    private ObservableSortedList<int, Entity> _exposures;

    // ── Interaction state ─────────────────────────────────────────────────────
    private int _hoveredFrame = -1; // -1 = none
    private int _pressedFrame = -1; // -1 = none

    // ── Theme ─────────────────────────────────────────────────────────────────
    public Color BarNormalColor;
    public Color BarHoverColor;
    public Color BarPressedColor;
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
        BarNormalColor = normalStyleBox.BgColor;
        var hoverStyleBox = (StyleBoxFlat)GetThemeStylebox("hover", "Button");
        BarHoverColor = hoverStyleBox.BgColor;
        var pressedStyleBox = (StyleBoxFlat)GetThemeStylebox("pressed", "Button");
        BarPressedColor = pressedStyleBox.BgColor;
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
        else if (what == NotificationMouseExit)
        {
            _hoveredFrame = -1;
            QueueRedraw();
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Observe(
        ReactiveProperty<float> pixelsPerFrame,
        ReactiveProperty<float> scrollOffsetFrame,
        ReactiveProperty<int> playbackStart,
        ReactiveProperty<int> playbackEnd,
        CompositeDisposable subs)
    {
        pixelsPerFrame.CombineLatest(scrollOffsetFrame, (ppf, sof) => (ppf, sof * ppf))
            .Subscribe(t =>
            {
                _ppf = t.ppf;
                _scrollOffset = t.Item2;
                QueueRedraw();
            }).AddTo(subs);
        playbackStart.Subscribe(v =>
        {
            _playbackStart = v;
            QueueRedraw();
        }).AddTo(subs);
        playbackEnd.Subscribe(v =>
        {
            _playbackEnd = v;
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

        float playbackStartX = _playbackStart * _ppf - _scrollOffset;
        float playbackEndX = _playbackEnd * _ppf - _scrollOffset;

        // ── Arrow from playbackStart to first in-range frame ──────────────────
        int firstInRange = -1;
        foreach (int f in frames)
            if (f >= _playbackStart && f < _playbackEnd) { firstInRange = f; break; }

        bool hasFramesBefore = frames.Count > 0 && frames[0] < _playbackStart;
        if (hasFramesBefore && firstInRange >= 0)
        {
            float shaftStart = playbackStartX;
            float firstX = firstInRange * _ppf - _scrollOffset;
            float tipX = Mathf.Min(firstX, w + ArrowHeadLength);
            if (tipX - shaftStart > ArrowHeadLength)
            {
                DrawLine(new(shaftStart, midY), new(tipX - ArrowHeadLength, midY), ArrowColor);
                Vector2 tip = new(tipX, midY);
                Vector2 p1 = new(tipX - ArrowHeadLength, midY - ArrowHeadHalfWidth);
                Vector2 p2 = new(tipX - ArrowHeadLength, midY + ArrowHeadHalfWidth);
                DrawColoredPolygon([tip, p1, p2], ArrowColor);
            }
        }

        for (int i = 0; i < frames.Count; i++)
        {
            int frame = frames[i];
            float x = frame * _ppf - _scrollOffset;

            // ── Cel drag bar
            var barRect = new Rect2(x, 0f, barW, h);
            if (barRect.End.X > 0f && barRect.Position.X < w)
            {
                Color barColor = frame == _pressedFrame ? BarPressedColor
                               : frame == _hoveredFrame ? BarHoverColor
                               : BarNormalColor;
                DrawRect(barRect, barColor);
            }

            // ── Layer name label (draw for any visible frame) ─────────────────
            var layerE = _exposures[frame];
            string name = layerE.Get<CommonLayerSetting>().Name.Value;
            float labelX = x + barW + LabelPad;
            // Label end: next frame's bar (any range), except last in-range frame uses playbackEndX
            int nextAny = (i + 1 < frames.Count) ? frames[i + 1] : -1;
            float labelEnd = (nextAny >= 0)
                ? nextAny * _ppf - _scrollOffset - ArrowHeadLength - LabelPad
                : w;
            float maxW = labelEnd - labelX;
            if (maxW > 0f && labelX < w)
                DrawString(LabelFont, new Vector2(labelX, midY + LabelFontSize * 0.35f),
                    name, HorizontalAlignment.Left, maxW, LabelFontSize, LabelColor);

            if (frame < _playbackStart || frame >= _playbackEnd) continue; // skip arrows for out-of-range frames

            // Next frame within playback range (or none)
            int nextInRange = -1;
            for (int j = i + 1; j < frames.Count; j++)
            {
                if (frames[j] >= _playbackStart && frames[j] < _playbackEnd) { nextInRange = frames[j]; break; }
            }

            // ── Arrow to next bar (or to playbackEnd for the last in-range frame) ────
            float shaftStart = x + barW;
            float tipX;

            if (nextInRange >= 0)
            {
                // Arrow points toward next bar
                float nextX = nextInRange * _ppf - _scrollOffset;
                tipX = Mathf.Min(nextX, w + ArrowHeadLength);
            }
            else
            {
                // Last in-range frame: arrow points to playbackEnd
                tipX = Mathf.Min(playbackEndX, w + ArrowHeadLength);
            }

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

    /// <summary>Returns the frame key whose bar contains <paramref name="posX"/>, or -1.</summary>
    private int FrameAt(float posX)
    {
        if (_ppf <= 0f || _exposures == null) return -1;
        float barW = BarWidth;
        foreach (var kv in _exposures)
        {
            float x = kv.Key * _ppf - _scrollOffset;
            if (posX >= x && posX < x + barW)
                return kv.Key;
        }
        return -1;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            int newHovered = FrameAt(motion.Position.X);
            if (newHovered != _hoveredFrame)
            {
                _hoveredFrame = newHovered;
                QueueRedraw();
            }
        }
        else if (@event is InputEventMouseButton btn && btn.ButtonIndex == MouseButton.Left)
        {
            if (btn.Pressed)
            {
                int f = FrameAt(btn.Position.X);
                if (f >= 0)
                {
                    _pressedFrame = f;
                    QueueRedraw();
                }
            }
            else
            {
                if (_pressedFrame >= 0)
                {
                    _pressedFrame = -1;
                    QueueRedraw();
                }
            }
        }
    }

    public override int _GetCursorShape(Vector2 atPosition) =>
        FrameAt(atPosition.X) >= 0 ? (int)CursorShape.PointingHand : (int)CursorShape.Arrow;
}