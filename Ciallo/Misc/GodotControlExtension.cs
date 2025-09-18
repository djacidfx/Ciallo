using System;
using System.Collections.Generic;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class GodotControlExtension
{
    public static IDisposable VisibleIf<T>(this Control control, ReactiveProperty<T> property, Predicate<T> predicate)
    {
        return property.Subscribe(value => control.Visible = predicate(value));
    }
    
    public static IDisposable VisibleIf<T>(this Control control, ReactiveProperty<T> property, T value)
    {
        return control.VisibleIf(property, v => EqualityComparer<T>.Default.Equals(v, value));
    }
}