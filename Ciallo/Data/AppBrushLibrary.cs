using System.Collections.Generic;
using System.Linq;
using Ciallo.Misc;
using Godot;
using Newtonsoft.Json;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public static class AppBrushLibrary
{
    public static readonly ReactiveProperty<BrushSetting> CurrentBrush = new(null);
    public static readonly ObservableList<BrushSetting> Brushes = [];

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
            Color = { Value = new(0,0,0,0.3f) },
            FalloffCurve = new([
                new(new(0,1), new(-0.25f,0), new(0.25f,0)),
                new(new(1,0), new(-0.25f,0), new(0.25f,0))
            ]),
        });

        return brushes;
    }

    public static void ResetBuiltInBrushes()
    {
        var userBrushes = Brushes.ToList();
        userBrushes.RemoveAll(b => b.Labels.Contains(BrushLabel.BuiltIn));
        var builtInBrushes = CreateBuiltInBrushes();
        Brushes.Clear();
        Brushes.AddRange(builtInBrushes);
        Brushes.AddRange(userBrushes);
    }

    public static readonly string Path = "user://Brush.json";

    public static void Save()
    {
        var content = JsonConvert.SerializeObject(AppBrushLibrary.Brushes, Preference.JsonOptions);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }

    public static bool TryLoad()
    {
        if (!FileAccess.FileExists(Path))
            return false;
        AppBrushLibrary.Brushes.Clear();
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content, AppBrushLibrary.Brushes, Preference.JsonOptions);
        return true;
    }
}