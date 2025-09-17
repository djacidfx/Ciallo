using System;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class GodotControlExtension
{
    public static IDisposable VisibleIf<T>(this Control control, ReactiveProperty<T> property, Predicate<T> predicate) where T : struct
    {
        return property.Subscribe(value => control.Visible = predicate(value));
    }
    
    public static IDisposable VisibleIf<T>(this Control control, ReactiveProperty<T> property, T value) where T : struct
    {
        return control.VisibleIf(property, v => v.Equals(value));
    }
}