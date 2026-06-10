using System;
using System.Collections.Generic;
using Godot;
using R3;

// ReSharper disable once CheckNamespace
namespace Ciallo;

public static class GodotControlExtension
{
    public static TControl VisibleIf<TControl>(this TControl control, Observable<bool> visible, CompositeDisposable disposables) where TControl : Control
    {
        var sub = visible.Subscribe(value => control.Visible = value);
        disposables.Add(sub);
        return control;
    }

    public static TControl VisibleIf<TControl>(this TControl control, Observable<bool> visible) where TControl : Control
    {
        var sub = visible.Subscribe(value => control.Visible = value);
        sub.AddTo(control);
        return control;
    }

    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, Predicate<T> predicate, CompositeDisposable disposables) where TControl : Control
    {
        var sub = property.Subscribe(value => control.Visible = predicate(value));
        disposables.Add(sub);
        return control;
    }

    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, Predicate<T> predicate) where TControl : Control
    {
        var sub = property.Subscribe(value => control.Visible = predicate(value));
        sub.AddTo(control);
        return control;
    }

    public static TControl VisibleIf<TControl, T>(this TControl control, Observable<T> property, T value) where TControl : Control
    {
        control.VisibleIf(property, v => EqualityComparer<T>.Default.Equals(v, value));
        return control;
    }
}
