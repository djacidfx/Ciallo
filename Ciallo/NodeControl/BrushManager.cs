using Godot;
using System;
using Ciallo.Data;
using ObservableCollections;
using R3;

public partial class BrushManager : PopupPanel
{
    public ReactiveProperty<BrushSetting> CurrentBrush = new(null);

    public override void _Ready()
    {
        
    }


    public static void AddBuiltInBrush()
    {
        
    }
}
