using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiControl;

public partial class ConfigureGlobalPenPressure : AcceptDialog
{
    private OptionButton _brandButton;
    private OptionButton _penButton;
    private OptionButton _tabletButton;
    private Button _straightenButton;

    // Left: device's raw digital response (gram -> 0..1). Right: response after the remap curve.
    private PolylineChart _rawChart;
    private PolylineChart _mappedChart;

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
        Title = "Configure Global Pen Pressure";

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(vbox);

        var selectors = new HBoxContainer();
        vbox.AddChild(selectors);
        _brandButton = new OptionButton();
        _penButton = new OptionButton();
        _tabletButton = new OptionButton();
        selectors.AddChild(_brandButton);
        selectors.AddChild(_penButton);
        selectors.AddChild(_tabletButton);

        // Solves for a remap curve that straightens the selected device's force -> pressure response.
        _straightenButton = new Button { Text = "Straighten" };
        selectors.AddChild(_straightenButton);

        // Three panels side by side: raw response, the editable remap curve, the composed result.
        var charts = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        vbox.AddChild(charts);

        // X (grams) pinned to origin 0 and integer-labelled; Y (0..1 digital) fixed to 0..1
        // so the ticks read 0 / 0.5 / 1.0 like the remap curve. Scatter: samples are discrete.
        _rawChart = new PolylineChart
        {
            FixedMinX = 0f, FixedMinY = 0f, FixedMaxY = 1f,
            XFormat = "0", YFormat = "0.0", Scatter = true, NiceNumbers = true,
        };
        var curveEdit = new MappingCurveEdit().BindCurve(AppPreference.PenPressureRemapCurve);
        _mappedChart = new PolylineChart
        {
            FixedMinX = 0f, FixedMinY = 0f, FixedMaxY = 1f,
            XFormat = "0", YFormat = "0.0", Scatter = true, NiceNumbers = true,
        };

        charts.AddChild(WrapTitled("Device response", _rawChart));
        charts.AddChild(WrapTitled("Global remap", curveEdit));
        charts.AddChild(WrapTitled("Mapped response", _mappedChart));

        _brandButton.ItemSelected += OnBrandSelected;
        _penButton.ItemSelected += OnPenSelected;
        _tabletButton.ItemSelected += OnTabletSelected;
        _straightenButton.Pressed += OnStraightenPressed;

        // The mapped chart depends on the remap curve, so re-plot it whenever the curve changes.
        AppPreference.PenPressureRemapCurve.Subscribe(_ => PlotMapped()).AddTo(_disposables);

        PopulateBrands();
    }

    private static Control WrapTitled(string title, Control content)
    {
        content.CustomMinimumSize = new Vector2(300, 300);
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(new Label { Text = title.Tr(), HorizontalAlignment = HorizontalAlignment.Center });
        box.AddChild(content);
        return box;
    }

    private void PopulateBrands()
    {
        var brands = PenPressureResponseLibrary.Brands();
        _brandButton.Clear();
        foreach (var brand in brands)
            _brandButton.AddItem(brand);

        if (brands.Count > 0)
        {
            _brandButton.Selected = 0;
            OnBrandSelected(0);
        }
    }

    private void OnBrandSelected(long index)
    {
        string brand = _brandButton.GetItemText((int)index);
        _pens = PenPressureResponseLibrary.PensOf(brand);

        _penButton.Clear();
        foreach (var pen in _pens)
            _penButton.AddItem(PenPressureEntry.ShortName(pen));

        if (_pens.Count > 0)
        {
            _penButton.Selected = 0;
            OnPenSelected(0);
        }
    }

    private void OnPenSelected(long index)
    {
        string brand = _brandButton.GetItemText(_brandButton.Selected);
        string pen = _pens[(int)index];
        _tablets = PenPressureResponseLibrary.TabletsOf(brand, pen);

        _tabletButton.Clear();
        foreach (var tablet in _tablets)
            _tabletButton.AddItem(PenPressureEntry.ShortName(tablet));

        if (_tablets.Count > 0)
        {
            _tabletButton.Selected = 0;
            OnTabletSelected(0);
        }
    }

    private void OnTabletSelected(long index)
    {
        string brand = _brandButton.GetItemText(_brandButton.Selected);
        string pen = _pens[_penButton.Selected];
        string tablet = _tablets[(int)index];
        _selected = PenPressureResponseLibrary.MatchLatest(brand, pen, tablet);
        PlotRaw();
        PlotMapped();
    }

    // Solves for a remap curve that straightens the selected device's force->pressure response,
    // then overwrites the global remap curve. The mapped chart re-plots automatically via the
    // PenPressureRemapCurve subscription, so the right panel is the visual verification.
    private void OnStraightenPressed()
    {
        if (_selected == null)
            return;
        var remap = PressureRemapSolver.Straighten(_selected.Records);
        if (remap is null)
        {
            GD.PushWarning("Cannot straighten: the selected device response is degenerate (no usable force span).");
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
        _rawChart.BindPolyline(property).AddTo(_rawPlot);
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
        _mappedChart.BindPolyline(property).AddTo(_mappedPlot);
    }

    public override void _ExitTree()
    {
        _rawPlot.Dispose();
        _mappedPlot.Dispose();
        _disposables.Dispose();
    }
}
