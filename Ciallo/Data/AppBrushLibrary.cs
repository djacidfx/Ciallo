using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Widget;
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

    public static void BindToGui()
    {
        // Setup brush library panel
        var panel = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<BrushPanel>().First();
        var view = Brushes.CreateWritableView(setting =>  setting.Name);
        view.AddTo(panel);
        panel.BrushSelector.BindValue(view, CurrentBrush);
        
        foreach (var brush in Brushes)
        {
            var propertyBox = new PropertyContainer();
            brush.DrawProperty(propertyBox);
            propertyBox.VisibleIf(CurrentBrush, brush);
            panel.PropertiesHolder.AddChild(propertyBox);
        }
        
        Brushes.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var brush = e.NewItem;
                    var propertyBox = new PropertyContainer();
                    brush.DrawProperty(propertyBox);
                    propertyBox.VisibleIf(CurrentBrush, brush);
                    panel.PropertiesHolder.AddChild(propertyBox);
                    panel.PropertiesHolder.MoveChild(propertyBox, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    panel.PropertiesHolder.GetChild(e.OldStartingIndex).QueueFree();
                    break;
                case NotifyCollectionChangedAction.Move:
                    panel.PropertiesHolder.MoveNode([e.OldStartingIndex], [e.NewStartingIndex]);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    panel.PropertiesHolder.QueueFreeChildren();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new("Should be unreachable");
            }
        }).AddTo(panel);

        int count = 1;
        panel.Add.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var newBrush = new BrushSetting()
            {
                Name = { Value = "New brush".Tr() + " " + count++},
            };
            Brushes.Add(newBrush);
            CurrentBrush.Value = newBrush;
        };
        
        panel.Remove.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var idx = Brushes.IndexOf(CurrentBrush.Value);
            Brushes.Remove(CurrentBrush.Value);
            if (Brushes.Count == 0)
                CurrentBrush.Value = null;
            else if (idx >= Brushes.Count)
                CurrentBrush.Value = Brushes.Last();
            else
                CurrentBrush.Value = Brushes[idx];
        };
        
        panel.Copy.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var newBrush = CurrentBrush.Value.Clone();
            newBrush.Name.Value += " " + count++;
            Brushes.Add(newBrush);
            CurrentBrush.Value = newBrush;
        };
        
        panel.Reset.Pressed += () =>
        {
            ResetBuiltInBrushes();
            if (!Brushes.Contains(CurrentBrush.Value))
                CurrentBrush.Value = Brushes[0];
        };
        
        panel.Up.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var brush = CurrentBrush.Value;
            var idx = Brushes.IndexOf(brush);
            if (idx <= 0) return;
            Brushes.Move(idx, idx - 1);
        };

        panel.Down.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var brush = CurrentBrush.Value;
            var idx = Brushes.IndexOf(brush);
            if (idx >= Brushes.Count - 1) return;
            Brushes.Move(idx, idx + 1);
        };

        panel.Top.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var brush = CurrentBrush.Value;
            var idx = Brushes.IndexOf(brush);
            if (idx <= 0) return;
            Brushes.Move(idx, 0);
        };

        panel.Bottom.Pressed += () =>
        {
            if (CurrentBrush.Value == null)
                return;
            var brush = CurrentBrush.Value;
            var idx = Brushes.IndexOf(brush);
            if (idx >= Brushes.Count - 1) return;
            Brushes.Move(idx, Brushes.Count - 1);
        };
    }
}