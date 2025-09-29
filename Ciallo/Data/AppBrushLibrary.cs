using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Ciallo.Widget;
using Godot;
using Newtonsoft.Json;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public static class AppBrushLibrary
{
    public static ReactiveProperty<int> SelectedIndex;
    public static readonly ObservableList<BrushSetting> BrushSettings = [];
    public static ReadOnlyReactiveProperty<BrushSetting> SelectedBrushSetting;

    public static bool HasSelection => SelectedBrushSetting?.CurrentValue != null;

    public static List<BrushSetting> CreateBuiltInBrushes()
    {
        List<BrushSetting> brushes = [];
        brushes.Add(new()
        {
            Name = { Value = "Solid".Tr()},
            RenderingType = { Value = BrushRenderingType.Vanilla },
            Labels = { BrushLabel.BuiltIn },
        });
        
        brushes.Add(new()
        {
            Name = { Value = "High performance".Tr() + " " + "Soft airbrush".Tr()},
            RenderingType = { Value = BrushRenderingType.Airbrush },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0,0,0,0.2f) },
            FalloffCurve = new([
                new(new(0,1), new(-0.25f,0), new(0.5f,0)),
                new(new(1,0), new(-0.25f,0), new(0.25f,0))
            ]),
        });
        
        brushes.Add(new()
        {
            Name = { Value = "High performance".Tr() + " " + "Hard airbrush".Tr()},
            RenderingType = { Value = BrushRenderingType.Airbrush },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0,0,0,0.3f) },
            FalloffCurve = new([
                new(new(0,1), new(-0.25f,0), new(0.65f,0)),
                new(new(1,0), new(0,0.25f), new(0.25f,0))
            ]),
        });

        return brushes;
    }

    public static void ResetBuiltInBrushes()
    {
        var userBrushes = BrushSettings.ToList();
        userBrushes.RemoveAll(b => b.Labels.Contains(BrushLabel.BuiltIn));
        var builtInBrushes = CreateBuiltInBrushes();
        BrushSettings.Clear();
        BrushSettings.AddRange(builtInBrushes);
        BrushSettings.AddRange(userBrushes);
    }

    public static readonly string Path = "user://Brush.json";

    public static void Save()
    {
        var content = JsonConvert.SerializeObject(BrushSettings, Preference.JsonOptions);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }

    public static bool TryLoad()
    {
        if (!FileAccess.FileExists(Path))
            return false;
        BrushSettings.Clear();
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content, BrushSettings, Preference.JsonOptions);
        return true;
    }

    public static void BindToGui()
    {
        // Setup brush library panel
        var panel = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<BrushPanel>().First();
        SelectedIndex = panel.SelectedIndex;
        panel.BindBrushSetting(BrushSettings, s => s);
        
        // Note about `BrushSettings.ObserveChanged().ToReadOnlyReactiveProperty()`
        // ToReadOnlyReactiveProperty() is necessary to trigger the initial value of observable.
        // Or CombineLatest lacks of the first value to get to work. Or use `Prepend` function.
        SelectedBrushSetting = SelectedIndex
            .CombineLatest(BrushSettings.ObserveChanged().ToReadOnlyReactiveProperty(), (idx, _) =>idx)
            .Select(idx => idx < 0 || idx >= BrushSettings.Count ? null : BrushSettings[idx])
            .ToReadOnlyReactiveProperty();

        // Create stroke preview
        var preview = new StrokeView();
        panel.BrushPreviewViewport.AddChild(preview);
        // Note: Lazy on clearing these caches on destruction. I don't believe user will view 1e5 brushes in one session.
        Dictionary<BrushSetting, BrushMaterial> materialCache = new();
        CompositeDisposable curveChangeSubs = new(); 
        curveChangeSubs.AddTo(panel);
        SelectedBrushSetting.Subscribe(setting =>
        {
            if (setting == null)
            {
                preview.Material = null;
                return;
            }
            materialCache.TryGetValue(setting, out var material);
            if (material == null)
            {
                material = new();
                material.ObserveBrushSetting(setting);
                materialCache[setting] = material;
                setting.Pressure2RadiusRatioCurve.Changed.Prepend(new Unit()).Subscribe(_ => 
                    UpdateStrokePreview(preview, setting.Pressure2RadiusRatioCurve)).AddTo(curveChangeSubs);
            }
            preview.Material = material;
        }).AddTo(panel);
        
        // Brush list operations and buttons
        int count = 1;
        panel.Add.Pressed += () =>
        {
            if (SelectedIndex.Value < 0)
                return;
            var newBrush = new BrushSetting()
            {
                Name = { Value = "New brush".Tr() + " " + count++},
            };
            BrushSettings.Add(newBrush);
            SelectedIndex.Value = BrushSettings.Count - 1;
        };
        
        panel.Remove.Pressed += () =>
        {
            if (SelectedIndex.Value < 0)
                return;
            var idx = SelectedIndex.Value;
            BrushSettings.RemoveAt(idx);
            if (BrushSettings.Count == 0)
                SelectedIndex.Value = -1;
            else if (idx >= BrushSettings.Count)
                SelectedIndex.Value = BrushSettings.Count - 1;
            else
                SelectedIndex.OnNext(idx);
        };
        
        panel.Copy.Pressed += () =>
        {
            int idx = SelectedIndex.Value;
            if (idx < 0) return;
            var newBrush = BrushSettings[idx].Clone();
            newBrush.Name.Value += " " + count++;
            BrushSettings.Add(newBrush);
            SelectedIndex.Value = BrushSettings.Count - 1;
        };
        
        panel.Reset.Pressed += () =>
        {
            int prev = SelectedIndex.Value;
            ResetBuiltInBrushes();
            if (BrushSettings.Count == 0)
                SelectedIndex.Value = -1;
            else if (prev < 0)
                SelectedIndex.Value = 0;
            else if (prev >= BrushSettings.Count)
                SelectedIndex.Value = BrushSettings.Count - 1;
            else
                SelectedIndex.OnNext(prev);
        };
        
        panel.Up.Pressed += () =>
        {
            int idx = SelectedIndex.Value;
            if (idx <= 0) return;
            BrushSettings.Move(idx, idx - 1);
            SelectedIndex.Value = idx - 1;
        };

        panel.Down.Pressed += () =>
        {
            int idx = SelectedIndex.Value;
            if (idx < 0 || idx >= BrushSettings.Count - 1) return;
            BrushSettings.Move(idx, idx + 1);
            SelectedIndex.Value = idx + 1;
        };

        panel.Top.Pressed += () =>
        {
            int idx = SelectedIndex.Value;
            if (idx <= 0) return;
            BrushSettings.Move(idx, 0);
            SelectedIndex.Value = 0;
        };

        panel.Bottom.Pressed += () =>
        {
            int idx = SelectedIndex.Value;
            if (idx < 0 || idx >= BrushSettings.Count - 1) return;
            BrushSettings.Move(idx, BrushSettings.Count - 1);
            SelectedIndex.Value = BrushSettings.Count - 1;
        };
    }

    private static void UpdateStrokePreview(StrokeView view, BezierCurve pressureCurve)
    {
        int n = 64;
        float gr = (1 + Mathf.Sqrt(5)) / 2; // golden ratio
        var ts = Enumerable.Range(0, n)
            .Select(i => i / (n - 1f))
            .Select(i => (i * 2 - 1f) * Mathf.Pi)
            .ToImmutableArray(); // [-pi, pi]
        
        var points = ts.Select(t => new Vector2(t, Mathf.Sin(t) / gr)).ToImmutableArray();
        var radii = ts
            .Select(t => Mathf.Cos(t / 2.0f))
            .Select(pressureCurve.SampleX)
            .Select(radiusRatio => radiusRatio * 0.5f/gr)
            .ToImmutableArray();
        view.SetGeometry(points, radii);
    }
}