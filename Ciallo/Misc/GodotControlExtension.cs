using System;
using System.Collections.Generic;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class GodotControlExtension
{
    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, Predicate<T> predicate, out IDisposable sub) where TControl : Control
    {
        sub = property.Subscribe(value => control.Visible = predicate(value));
        return control;
    }

    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, Predicate<T> predicate) where TControl : Control
    {
        control.VisibleIf(property, predicate, out var sub);
        sub.AddTo(control);
        return control;
    }

    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, T value) where TControl : Control
    {
        control.VisibleIf(property, v => EqualityComparer<T>.Default.Equals(v, value));
        return control;
    }
}