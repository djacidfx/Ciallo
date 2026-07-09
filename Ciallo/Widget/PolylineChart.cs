/* A read-only chart control that plots one or more polylines on the same background
   as MappingCurveEdit (panel stylebox + grid + axis labels), but without any editing
   interaction. The axes auto-fit to the bounding box of all bound polylines.

   Shen's "糊上一层" plan: today we only draw a single polyline and a single point, but
   the binding API already accepts many so a third chart type isn't needed for a while. */

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Godot;
using R3;

namespace Ciallo.Widget;

/// <summary>
/// Plots polylines (in world/data coordinates) on an auto-fitted set of axes.
/// Shows nothing until at least one <see cref="BindPolyline"/> call feeds it data.
/// </summary>
[Tool, GlobalClass]
public partial class PolylineChart : Control
{
    private const float LineWidth = 1.0f;
    private const float PointRadius = 4f;
    private const float ScatterRadius = 2.5f;

    // Breathing room around the data box (fraction of each axis range) so the line
    // doesn't hug the borders. Applied after fitting to the polylines.
    private const float FitPaddingRatio = 0.05f;

    // ---- Axis configuration (set before binding). ------------------------
    // A non-null fixed edge overrides auto-fit for that edge and is used verbatim (no padding),
    // e.g. FixedMinX=0 pins the origin to 0, FixedMinY=0/FixedMaxY=1 gives a 0..0.5..1 Y axis.
    public float? FixedMinX { get; set; }
    public float? FixedMaxX { get; set; }
    public float? FixedMinY { get; set; }
    public float? FixedMaxY { get; set; }

    // Tick-label number formats (see IFormattable). X rounded to integer, Y to 0.1, by default generic.
    public string XFormat { get; set; } = "0.##";
    public string YFormat { get; set; } = "0.##";

    // Draw each sample as a dot instead of connecting them into a line.
    public bool Scatter { get; set; }

    // Snap each auto-fitted (non-fixed) axis edge outward to a "nice" round multiple and pick a
    // nice tick step (Heckbert). Widens the range a little in exchange for round tick labels.
    public bool NiceNumbers { get; set; }

    // Target number of tick intervals per axis (actual count floats once snapped to nice steps).
    private const int TargetIntervalsX = 4;
    private const int TargetIntervalsY = 2;

    private sealed class PolylineEntry(ReactiveProperty<ImmutableArray<Vector2>> property, Color? color)
    {
        public ReactiveProperty<ImmutableArray<Vector2>> Property { get; } = property;
        // Null means "resolve to the theme font color at draw time" (theme may not be ready at bind time).
        public Color? Color { get; } = color;
    }

    private readonly List<PolylineEntry> _polylines = [];

    private ReactiveProperty<Vector2?> _point;
    private Color? _pointColor;

    private Transform2D _worldToView;

    // Fitted world bounds (already padded/snapped). Default to the unit box so an empty chart still draws.
    private float _minX, _maxX = 1f, _minY, _maxY = 1f;
    // Tick step per axis, computed alongside the bounds.
    private float _stepX = 0.25f, _stepY = 0.5f;
    private float DomainRange => _maxX - _minX;
    private float ValueRange => _maxY - _minY;

    public PolylineChart()
    {
        ClipContents = true;
        Resized += QueueRedraw;
    }

    public override Vector2 _GetMinimumSize() => new(256, 256);

    /// <summary>
    /// Adds a polyline to the chart. The returned disposable removes it again when disposed,
    /// so callers can add/remove layers over the chart's lifetime.
    /// </summary>
    /// <param name="color">Line color; null falls back to the theme's label font color.</param>
    public CompositeDisposable BindPolyline(ReactiveProperty<ImmutableArray<Vector2>> property, Color? color = null)
    {
        var entry = new PolylineEntry(property, color);
        _polylines.Add(entry);

        var disposables = new CompositeDisposable();
        property.Subscribe(_ =>
        {
            RecomputeBounds();
            QueueRedraw();
        }).AddTo(disposables);

        Disposable.Create(() =>
        {
            _polylines.Remove(entry);
            // The chart may already be freed if a caller disposes bindings during teardown.
            if (GodotObject.IsInstanceValid(this))
            {
                RecomputeBounds();
                QueueRedraw();
            }
        }).AddTo(disposables);

        return disposables;
    }

    /// <summary>
    /// Binds a single highlighted point drawn on top of the polylines. The point does not
    /// participate in axis fitting, so a live/moving indicator won't rescale the chart.
    /// Null hides the point.
    /// </summary>
    public CompositeDisposable BindPoint(ReactiveProperty<Vector2?> point, Color? color = null)
    {
        _point = point;
        _pointColor = color;

        var disposables = new CompositeDisposable();
        point.Subscribe(_ => QueueRedraw()).AddTo(disposables);

        Disposable.Create(() =>
        {
            if (_point == point)
                _point = null;
            if (GodotObject.IsInstanceValid(this))
                QueueRedraw();
        }).AddTo(disposables);

        return disposables;
    }

    private void RecomputeBounds()
    {
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

        foreach (var entry in _polylines)
        {
            var points = entry.Property.Value;
            if (points.IsDefault)
                continue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.X);
                maxX = Mathf.Max(maxX, p.X);
                minY = Mathf.Min(minY, p.Y);
                maxY = Mathf.Max(maxY, p.Y);
            }
        }

        // No finite data: fall back to a neutral unit box (still honoring fixed edges).
        bool empty = float.IsInfinity(minX) || float.IsInfinity(minY);
        if (empty) { minX = 0f; maxX = 1f; minY = 0f; maxY = 1f; }

        (_minX, _maxX, _stepX) = ComputeAxis(minX, maxX, FixedMinX, FixedMaxX, TargetIntervalsX);
        (_minY, _maxY, _stepY) = ComputeAxis(minY, maxY, FixedMinY, FixedMaxY, TargetIntervalsY);
    }

    /// <summary>
    /// Resolves one axis to (min, max, tickStep). Fixed edges win verbatim. When <see cref="NiceNumbers"/>
    /// is on, the step is snapped to a 1/2/5x10^n value and the free edges are rounded outward to a
    /// multiple of it (so labels land on round numbers, at the cost of a slightly wider range).
    /// Otherwise the free edges get symmetric padding and the range is split into equal intervals.
    /// </summary>
    private (float min, float max, float step) ComputeAxis(
        float dataMin, float dataMax, float? fixedMin, float? fixedMax, int targetIntervals)
    {
        float lo = fixedMin ?? dataMin;
        float hi = fixedMax ?? dataMax;
        float span = hi - lo;
        if (span <= 0f || !float.IsFinite(span))
            span = Mathf.IsZeroApprox(hi) ? 1f : Mathf.Abs(hi) * 0.1f;

        if (!NiceNumbers)
        {
            float pad = span * FitPaddingRatio;
            float min = fixedMin ?? dataMin - pad;
            float max = fixedMax ?? dataMax + pad;
            if (max <= min) max = min + span;
            return (min, max, (max - min) / targetIntervals);
        }

        float step = NiceNum(span / targetIntervals, round: true);
        if (step <= 0f) step = 1f;
        float niceMin = fixedMin ?? Mathf.Floor(dataMin / step) * step;
        float niceMax = fixedMax ?? Mathf.Ceil(dataMax / step) * step;
        if (niceMax <= niceMin) niceMax = niceMin + step;
        return (niceMin, niceMax, step);
    }

    // Heckbert's "nice number" pick: the closest 1/2/5 x 10^n to <paramref name="range"/>.
    private static float NiceNum(float range, bool round)
    {
        if (range <= 0f || !float.IsFinite(range)) return 1f;
        float exp = Mathf.Floor(Mathf.Log(range) / Mathf.Log(10f));
        float pow = Mathf.Pow(10f, exp);
        float f = range / pow; // in [1, 10)
        float nf = round
            ? (f < 1.5f ? 1f : f < 3f ? 2f : f < 7f ? 5f : 10f)
            : (f <= 1f ? 1f : f <= 2f ? 2f : f <= 5f ? 5f : 10f);
        return nf * pow;
    }

    private void UpdateViewTransform()
    {
        float fontSize = (int)(GetThemeFontSize("font_size", "Label") * 0.8f);
        float margin = fontSize + 8;

        Rect2 worldRect = new(_minX, _minY, DomainRange, ValueRange);
        Vector2 viewSize = Size - new Vector2(margin * 2, margin * 2);
        Vector2 scale = viewSize / worldRect.Size;

        Transform2D worldTrans = Transform2D.Identity;
        worldTrans = worldTrans.Translated(-worldRect.Position - new Vector2(0, worldRect.Size.Y));
        worldTrans = worldTrans.Scaled(new Vector2(scale.X, -scale.Y));

        Transform2D viewTrans = Transform2D.Identity;
        viewTrans = viewTrans.Translated(new Vector2(margin, margin));

        _worldToView = viewTrans * worldTrans;
    }

    private Vector2 GetViewPos(Vector2 worldPos) => _worldToView * worldPos;
    private Vector2 GetWorldPos(Vector2 viewPos) => _worldToView.AffineInverse() * viewPos;

    public override void _Draw()
    {
        if (_polylines.Count == 0)
            return;

        UpdateViewTransform();

        // Background
        DrawStyleBox(GetThemeStylebox("panel", "Tree"), new Rect2(Vector2.Zero, Size));

        // Grid (drawn in world space)
        DrawSetTransformMatrix(_worldToView);

        Vector2 minEdge = GetWorldPos(new Vector2(0, Size.Y));
        Vector2 maxEdge = GetWorldPos(new Vector2(Size.X, 0));

        Color gridColorPrimary = GetThemeColor("font_color", "Label") * new Color(1, 1, 1, 0.25f);
        Color gridColor = GetThemeColor("font_color", "Label") * new Color(1, 1, 1, 0.1f);

        // Tick counts float once the step is snapped to nice numbers; clamp so a degenerate
        // step can never spin a huge draw loop.
        int ticksX = Mathf.Clamp(Mathf.RoundToInt(DomainRange / _stepX), 1, 50);
        int ticksY = Mathf.Clamp(Mathf.RoundToInt(ValueRange / _stepY), 1, 50);

        for (int i = 0; i <= ticksX; i++)
        {
            float x = _minX + i * _stepX;
            DrawLine(new Vector2(x, minEdge.Y), new Vector2(x, maxEdge.Y),
                i == 0 || i == ticksX ? gridColorPrimary : gridColor);
        }
        for (int i = 0; i <= ticksY; i++)
        {
            float y = _minY + i * _stepY;
            DrawLine(new Vector2(minEdge.X, y), new Vector2(maxEdge.X, y),
                i == 0 || i == ticksY ? gridColorPrimary : gridColor);
        }

        // Number markings (screen space)
        DrawSetTransformMatrix(Transform2D.Identity);

        Font font = GetThemeFont("font", "Label");
        int fontSize = (int)(GetThemeFontSize("font_size", "Label") * 0.8f);
        float fontHeight = font.GetHeight(fontSize);
        Color textColor = GetThemeColor("font_color", "Label");
        int pad = 2;

        for (int i = 0; i <= ticksX; i++)
        {
            float x = _minX + i * _stepX;
            DrawString(font, GetViewPos(new Vector2(x, _minY)) + new Vector2(pad, fontHeight - pad),
                Fmt(x, XFormat), HorizontalAlignment.Center, -1, fontSize, textColor);
        }
        for (int i = 0; i <= ticksY; i++)
        {
            float y = _minY + i * _stepY;
            DrawString(font, GetViewPos(new Vector2(_minX, y)) + new Vector2(pad, -pad),
                Fmt(y, YFormat), HorizontalAlignment.Left, -1, fontSize, textColor);
        }

        // Polylines (or scatter dots)
        Color fallback = GetThemeColor("font_color", "Label");
        foreach (var entry in _polylines)
        {
            var points = entry.Property.Value;
            if (points.IsDefault || points.Length == 0)
                continue;
            Color color = entry.Color ?? fallback;
            if (Scatter)
            {
                foreach (var p in points)
                    DrawCircle(GetViewPos(p), ScatterRadius, color);
            }
            else
            {
                for (int i = 1; i < points.Length; i++)
                    DrawLine(GetViewPos(points[i - 1]), GetViewPos(points[i]), color, LineWidth, true);
            }
        }

        // Highlighted point
        if (_point is { Value: { } pt })
        {
            Color color = _pointColor ?? Colors.Orange;
            DrawCircle(GetViewPos(pt), PointRadius, color);
        }
    }

    // Format a tick label with the per-axis format (X to integer, Y to 0.1, etc.).
    private static string Fmt(float v, string format) => v.ToString(format, CultureInfo.InvariantCulture);
}
