using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using Newtonsoft.Json;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class AppBrushLibrary : AcceptDialog
{
    public Container PropertiesHolder;
    public static readonly ReactiveProperty<BrushSetting> CurrentBrush = new(null);
    public static readonly ObservableList<BrushSetting> Brushes = [];

    public override void _Ready()
    {
        GetOkButton().Visible = false;
        var view = Brushes.CreateWritableView(setting =>  setting.Name);
        view.AddTo(this);
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");
        GetNode<BrushSelector>("%BrushSelector").BindValue(view, CurrentBrush);

        foreach (var brush in Brushes)
        {
            var propertyBox = new PropertyContainer();
            brush.DrawProperty(propertyBox);
            propertyBox.VisibleIf(CurrentBrush, brush);
            PropertiesHolder.AddChild(propertyBox);
        }
    }
    
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
        var content = JsonConvert.SerializeObject(Brushes, Preference.JsonOptions);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }

    public static bool TryLoad()
    {
        if (!FileAccess.FileExists(Path))
            return false;
        Brushes.Clear();
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content, Brushes, Preference.JsonOptions);
        return true;
    }
}
