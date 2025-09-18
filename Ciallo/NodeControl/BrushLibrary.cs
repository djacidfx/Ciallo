using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class BrushLibrary : PopupPanel
{
    public ReactiveProperty<BrushSetting> CurrentBrush = new(null);

    public override void _Ready()
    {
        
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
            Name = { Value = "High performance".Tr() + "Soft airbrush".Tr()},
            RenderingType = { Value = BrushRenderingType.Airbrush },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0,0,0,0.3f) },
            FalloffCurve = new([
                new(new(1,1), new(-0.25f,0), new(0.25f,0)),
                new(new(0,0), new(-0.25f,0), new(0.25f,0))
            ]),
        });

        return brushes;
    }

    public static void ResetBuiltInBrushes()
    {
        var userBrushes = AppPreference.Brushes.ToList();
        userBrushes.RemoveAll(b => b.Labels.Contains(BrushLabel.BuiltIn));
        var builtInBrushes = CreateBuiltInBrushes();
        AppPreference.Brushes.Clear();
        AppPreference.Brushes.AddRange(builtInBrushes);
        AppPreference.Brushes.AddRange(userBrushes);
    }
}
