using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Godot;
using MessagePack;
using ObservableCollections;
using R3;
using FileAccess = Godot.FileAccess;

namespace Ciallo.Data;

public static class AppBrushLibrary
{
    public static ReactiveProperty<int> SelectedIndex;
    public static readonly ObservableList<StrokeBrushSetting> BrushSettings = [];
    public static ReadOnlyReactiveProperty<StrokeBrushSetting> SelectedBrushSetting;

    public static bool HasSelection => SelectedBrushSetting?.CurrentValue != null;

    public static List<StrokeBrushSetting> CreateBuiltInBrushes()
    {
        List<StrokeBrushSetting> brushes = [];
        brushes.Add(new()
        {
            Name = { Value = "Solid".Tr() },
            RenderingType = { Value = BrushRenderingType.Vanilla },
            Labels = { BrushLabel.BuiltIn },
        });

        brushes.Add(new()
        {
            Name = { Value = "High performance".Tr() + " " + "Soft airbrush".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            BaseRadius = { Value = 12f },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0, 0, 0, 0.4f) },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2FlowCurve = BezierCurve.EaseInOut(),
            FalloffCurve = new([
                new(new(0, 1), new(-0.25f, 0), new(0.5f, 0)),
                new(new(1, 0), new(-0.25f, 0), new(0.25f, 0))
            ]),
        });

        brushes.Add(new()
        {
            Name = { Value = "High performance".Tr() + " " + "Hard airbrush".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            BaseRadius = { Value = 12f },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0, 0, 0, 0.9f) },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2FlowCurve = BezierCurve.EaseInOut(),
            FalloffCurve = new([
                new(new(0, 1), new(-0.25f, 0), new(0.65f, 0)),
                new(new(1, 0), new(0, 0.25f), new(0.25f, 0))
            ]),
        });

        var dirPath = "res://Rendering/Image/";
        Image[] images =
        [
            GD.Load<Image>(dirPath + "StampPencil.png"),
            GD.Load<Image>(dirPath + "StampSplatter.png"),
            GD.Load<Image>(dirPath + "FBMNoise.png")
        ];
        foreach (var image in images)
        {
            image.GenerateMipmaps();
        }

        brushes.Add(new()
        {
            Name = { Value = "Pencil".Tr() },
            RenderingType = { Value = BrushRenderingType.Stamp },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = Colors.Black },
            ActiveStampFlags = { Value = StampFlags.StampTexture | StampFlags.MaskTexture | StampFlags.RotationNoise },
            StampTexture = { Value = ImageTexture.CreateFromImage(images[0]) },
            StampInterval = { Value = 0.5f },
            MaskTexture = { Value = ImageTexture.CreateFromImage(images[2]) },
            RotationNoiseAmplitude = { Value = 8 * Mathf.Pi },
            RotationNoiseFrequency = { Value = 0.343234f },
        });

        brushes.Add(new()
        {
            Name = { Value = "Splatter".Tr() },
            RenderingType = { Value = BrushRenderingType.Stamp },
            Labels = { BrushLabel.BuiltIn },
            ActiveStampFlags = { Value = StampFlags.StampTexture | StampFlags.RotationNoise },
            StampTexture = { Value = ImageTexture.CreateFromImage(images[1]) },
            RotationNoiseAmplitude = { Value = Mathf.Pi },
            RotationNoiseFrequency = { Value = 0.5f },
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

    public static readonly string BrushFolder = "user://Brush/";

    private static string SanitizeFileName(string fileName)
    {
        var invalids = Path.GetInvalidFileNameChars();
        foreach (var c in invalids)
            fileName = fileName.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "Unknown name brush";
        return fileName;
    }

    public static void Save()
    {
        // Ensure folder exists
        using var baseDir = DirAccess.Open("user://");
        if (!baseDir.DirExists("Brush"))
            baseDir.MakeDir("Brush");

        // Clear
        using var dirAccess = DirAccess.Open(BrushFolder);
        dirAccess.ListDirBegin();
        string fileName;
        while ((fileName = dirAccess.GetNext()) != "")
            if (fileName.EndsWith(".bin"))
                dirAccess.Remove(fileName);
        dirAccess.ListDirEnd();

        HashSet<string> seenNames = new();
        // Save
        List<string> fileNames = [];
        foreach (var brush in BrushSettings)
        {
            var name = SanitizeFileName(brush.Name.Value);
            if (seenNames.Contains(name))
            {
                int suffix = 1;
                while (seenNames.Contains(name + "_" + suffix))
                    suffix++;
                name = name + "_" + suffix;
            }
            var path = BrushFolder + name + ".bin";
            seenNames.Add(name);

            var content = MessagePackSerializer.Serialize(brush);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            file.StoreBuffer(content);
            fileNames.Add(name);
        }
        using var manifest = FileAccess.Open(BrushFolder + "manifest", FileAccess.ModeFlags.Write);
        manifest.StoreBuffer(MessagePackSerializer.Serialize(fileNames));
    }

    public static bool TryLoad()
    {
        // Check folder
        using var baseDir = DirAccess.Open("user://");
        if (!baseDir.DirExists("Brush")) return false;

        // Get manifest
        List<string> manifestFileNames = [];
        if (FileAccess.FileExists(BrushFolder + "manifest"))
        {
            using var file = FileAccess.Open(BrushFolder + "manifest", FileAccess.ModeFlags.Read);
            var manifestByte = file.GetBuffer((long)file.GetLength());
            manifestFileNames = MessagePackSerializer.Deserialize<List<string>>(manifestByte);
        }

        // List files
        using var brushDir = DirAccess.Open(BrushFolder);
        var fileNames = new List<string>();
        brushDir.ListDirBegin();
        string fileEntry;
        while ((fileEntry = brushDir.GetNext()) != "")
            if (fileEntry.EndsWith(".bin"))
                fileNames.Add(fileEntry.GetBaseName());
        brushDir.ListDirEnd();

        if (fileNames.Count == 0) return false;

        // Load
        BrushSettings.Clear();
        BrushSettings.AddRange(Enumerable.Repeat<StrokeBrushSetting>(null, manifestFileNames.Count));

        foreach (var fn in fileNames)
        {
            using var file = FileAccess.Open(BrushFolder + fn + ".bin", FileAccess.ModeFlags.Read);
            var content = file.GetBuffer((long)file.GetLength());
            StrokeBrushSetting strokeBrush;
            try
            {
                strokeBrush = MessagePackSerializer.Deserialize<StrokeBrushSetting>(content);
            }
            catch (Exception)
            {
                continue;
            }
            var index = manifestFileNames.IndexOf(fn);
            if (index >= 0)
                BrushSettings[index] = strokeBrush;
            else
                BrushSettings.Add(strokeBrush);
        }
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
            .CombineLatest(BrushSettings.ObserveChanged().ToReadOnlyReactiveProperty(), (idx, _) => idx)
            .Select(idx => idx < 0 || idx >= BrushSettings.Count ? null : BrushSettings[idx])
            .ToReadOnlyReactiveProperty();

        // Create stroke preview
        var preview = new StrokeView();
        panel.BrushPreviewViewport.AddChild(preview);
        // Note: Lazy on clearing these caches on destruction. I don't believe user will view 1e5 brushes in one session.
        Dictionary<StrokeBrushSetting, StrokeBrushMaterial> materialCache = new();
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

                setting.BaseRadius.CombineLatest(setting.Pressure2RadiusCurve.Changed.Prepend(new Unit()), (r, _) => r)
                    .Subscribe(r => UpdateStrokePreview(preview, setting.Pressure2RadiusCurve, r)).AddTo(curveChangeSubs);
            }
            preview.Material = material;
        }).AddTo(panel);

        // Brush list operations and buttons
        int count = 1;
        panel.Add.Pressed += () =>
        {
            var newBrush = new StrokeBrushSetting()
            {
                Name = { Value = "New brush".Tr() + " " + count++ },
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

    private static void UpdateStrokePreview(StrokeView view, BezierCurve pressureCurve, float baseRadius = 0)
    {
        int n = 64;
        float gr = (1 + Mathf.Sqrt(5)) / 2; // golden ratio
        var xs = Enumerable.Range(0, n)
            .Select(i => i / (n - 1f))
            .Select(i => (i * 2 - 1f) * Mathf.Pi)
            .ToImmutableArray(); // [-pi, pi]

        var positions = xs.Select(x => new Vector2(x, Mathf.Sin(x) / gr)).ToImmutableArray();
        // prefix sum on length
        var lengths = new float[positions.Length];
        for (int i = 0; i < positions.Length - 1; i++)
        {
            var p0 = positions[i];
            var p1 = positions[i + 1];
            var l = (p1 - p0).Length();
            lengths[i + 1] = lengths[i] + l;
        }
        var midL = lengths[^1] / 2;
        var pressures = lengths
            .Select(l => (l - midL) / midL * float.Pi * 0.5f)
            .Select(Mathf.Cos)
            .ToImmutableArray();
        float targetRadius = baseRadius.SigmoidRemap(2.0f, 16f, 0.25f / gr, 0.75f / gr);
        var radii = pressures
            .Select(pressureCurve.SampleX)
            .Select(radiusRatio => radiusRatio * targetRadius)
            .ToImmutableArray();
        view.SetGeometry(positions, radii, pressures);
    }
}