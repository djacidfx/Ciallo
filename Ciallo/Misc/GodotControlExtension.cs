using System;
using System.Collections.Generic;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class GodotControlExtension
{
    public static void VisibleIf<T>(this Control control, ReadOnlyReactiveProperty<T> property, Predicate<T> predicate, out IDisposable sub)
    {
        sub = property.Subscribe(value => control.Visible = predicate(value));
    }
    
    public static void VisibleIf<T>(this Control control, ReadOnlyReactiveProperty<T> property, Predicate<T> predicate)
    {
        control.VisibleIf(property, predicate, out var sub);
        sub.AddTo(control);
    }
    
    public static void VisibleIf<T>(this Control control, ReadOnlyReactiveProperty<T> property, T value)
    {
        control.VisibleIf(property, v => EqualityComparer<T>.Default.Equals(v, value));
    }
}