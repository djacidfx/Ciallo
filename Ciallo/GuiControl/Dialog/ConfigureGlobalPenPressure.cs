using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;
using R3;

namespace Ciallo.GuiControl;

// Layout lives in ConfigureGlobalPenPressure.tscn; [SceneTree] generates the typed node
// accessors (BrandButton, RawChart, Scribble, ...) from the uniquely-named scene nodes.
[SceneTree]
public partial class ConfigureGlobalPenPressure : AcceptDialog
{
    // Current cascade selections. Downstream lists are recomputed whenever an upstream value changes.
    private IReadOnlyList<string> _pens = [];
    private IReadOnlyList<string> _tablets = [];

    // The latest matching entry for the current triple; kept so the mapped chart can be recomputed on curve edits.
    private PenPressureEntry _selected;

    private readonly CompositeDisposable _rawPlot = new();
    private readonly CompositeDisposable _mappedPlot = new();
    private readonly CompositeDisposable _disposables = new();

    public override void _Ready()
    {
        // Chart display options aren't [Export], so they're configured here rather than in the scene.
        // X (grams) pinned to origin 0 and integer-labelled; Y (0..1 digital) fixed to 0..1 so the
        // ticks read 0 / 0.5 / 1.0 like the remap curve. Scatter: samples are discrete.
        foreach (var chart in new[] { RawChart, MappedChart })
        {
            chart.FixedMinX = 0f;
            chart.FixedMinY = 0f;
            chart.FixedMaxY = 1f;
            chart.XFormat = "0";
            chart.YFormat = "0.0";
            chart.Scatter = true;
            chart.NiceNumbers = true;
        }

        CurveEdit.BindCurve(AppPreference.PenPressureRemapCurve);

        // Scribble pad: live pressure readout + radius sliders + clear.
        Scribble.PressureSampled += UpdatePressureReadout;
        UpdatePressureReadout(0f, 0f);
        MinRadiusSlider.BindNumber(Scribble.MinRadius);
        MaxRadiusSlider.BindNumber(Scribble.MaxRadius);
        ClearButton.Pressed += Scribble.Clear;

        // Wipe scribbles whenever the dialog is closed, so it reopens blank.
        VisibilityChanged += () =>
        {
            if (!Visible) Scribble.Clear();
        };

        BrandButton.ItemSelected += OnBrandSelected;
        PenButton.ItemSelected += OnPenSelected;
        TabletButton.ItemSelected += OnTabletSelected;
        StraightenButton.Pressed += OnStraightenPressed;

        // The mapped chart depends on the remap curve, so re-plot it whenever the curve changes.
        AppPreference.PenPressureRemapCurve.Subscribe(_ => PlotMapped()).AddTo(_disposables);

        PopulateBrands();
    }

    private void UpdatePressureReadout(float raw, float mapped) =>
        PressureReadout.Text = $"pen pressure  raw {raw:F2}  →  mapped {mapped:F2}";

    private void PopulateBrands()
    {
        var brands = PenPressureResponseLibrary.Brands();
        BrandButton.Clear();
        // Index 0 is an empty "no brand" entry: selecting it hides both charts.
        BrandButton.AddItem("");
        foreach (var brand in brands)
            BrandButton.AddItem(brand);

        // Start with no brand selected so the charts stay hidden until the user picks one.
        BrandButton.Selected = 0;
        OnBrandSelected(0);
    }

    private void OnBrandSelected(long index)
    {
        // The empty entry at index 0 means "no device"; hide the charts and clear downstream.
        if (index == 0)
        {
            _selected = null;
            _pens = [];
            _tablets = [];
            PenButton.Clear();
            TabletButton.Clear();
            PenButton.Disabled = true;
            TabletButton.Disabled = true;
            StraightenButton.Disabled = true;
            RawPanel.Visible = false;
            MappedPanel.Visible = false;
            return;
        }

        PenButton.Disabled = false;
        TabletButton.Disabled = false;
        StraightenButton.Disabled = false;
        RawPanel.Visible = true;
        MappedPanel.Visible = true;

        string brand = BrandButton.GetItemText((int)index);
        _pens = PenPressureResponseLibrary.PensOf(brand);

        PenButton.Clear();
        foreach (var pen in _pens)
            PenButton.AddItem(PenPressureEntry.ShortName(pen));

        if (_pens.Count > 0)
        {
            PenButton.Selected = 0;
            OnPenSelected(0);
        }
    }

    private void OnPenSelected(long index)
    {
        string brand = BrandButton.GetItemText(BrandButton.Selected);
        string pen = _pens[(int)index];
        _tablets = PenPressureResponseLibrary.TabletsOf(brand, pen);

        TabletButton.Clear();
        foreach (var tablet in _tablets)
            TabletButton.AddItem(PenPressureEntry.ShortName(tablet));

        if (_tablets.Count > 0)
        {
            TabletButton.Selected = 0;
            OnTabletSelected(0);
        }
    }

    private void OnTabletSelected(long index)
    {
        string brand = BrandButton.GetItemText(BrandButton.Selected);
        string pen = _pens[PenButton.Selected];
        string tablet = _tablets[(int)index];
        _selected = PenPressureResponseLibrary.MatchLatest(brand, pen, tablet);
        PlotRaw();
        OnStraightenPressed();
    }

    // Solves for a remap curve that straightens the selected device's force->pressure response,
    // preserving the current curve's terminal X as the full-pressure cutoff. The mapped chart
    // re-plots automatically via the PenPressureRemapCurve subscription.
    private void OnStraightenPressed()
    {
        float maxReading = AppPreference.PenPressureRemapCurve.Value[^1].P.X;
        var remap = PressureRemapSolver.Straighten(_selected.Records, maxReading);
        if (remap is null)
        {
            GD.PushWarning("Cannot straighten: the selected device response is degenerate (no usable force span).");
            PlotMapped();
            return;
        }
        AppPreference.PenPressureRemapCurve.Value = remap.Value;
    }

    private void PlotRaw()
    {
        _rawPlot.Clear();
        if (_selected == null)
            return;
        var property = new ReactiveProperty<ImmutableArray<Vector2>>(_selected.Records);
        RawChart.BindPolyline(property).AddTo(_rawPlot);
    }

    // Composes each raw sample (gram, digital01) through the remap curve -> (gram, remapped01).
    private void PlotMapped()
    {
        _mappedPlot.Clear();
        if (_selected == null)
            return;
        var curve = AppPreference.PenPressureRemapCurve.Value;
        var mapped = _selected.Records
            .Select(p => new Vector2(p.X, curve.SampleX(p.Y)))
            .ToImmutableArray();
        var property = new ReactiveProperty<ImmutableArray<Vector2>>(mapped);
        MappedChart.BindPolyline(property).AddTo(_mappedPlot);
    }

    public override void _ExitTree()
    {
        _rawPlot.Dispose();
        _mappedPlot.Dispose();
        _disposables.Dispose();
    }
}
