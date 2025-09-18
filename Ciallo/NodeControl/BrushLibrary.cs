using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class BrushLibrary : PopupPanel
{
    public Container PropertiesHolder;
    public readonly ReactiveProperty<BrushSetting> CurrentBrush = new(null);

    public override void _Ready()
    {
        var view = AppPreference.Brushes.CreateWritableView(setting =>  setting.Name.Value);
        view.AddTo(this);
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");
        GetNode<BrushSelector>("%BrushSelector").BindValue(view, CurrentBrush).AddTo(this);

        foreach (var brush in AppPreference.Brushes)
        {
            var propertyBox = new PropertyContainer();
            brush.DrawProperty(propertyBox);
            propertyBox.VisibleIf(CurrentBrush, brush).AddTo(this);
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
        var userBrushes = AppPreference.Brushes.ToList();
        userBrushes.RemoveAll(b => b.Labels.Contains(BrushLabel.BuiltIn));
        var builtInBrushes = CreateBuiltInBrushes();
        AppPreference.Brushes.Clear();
        AppPreference.Brushes.AddRange(builtInBrushes);
        AppPreference.Brushes.AddRange(userBrushes);
    }
}
