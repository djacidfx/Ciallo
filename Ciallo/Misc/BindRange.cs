using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindRange
{
    private static CompositeDisposable BindValue<T>(Godot.Range rangeControl, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        // Note: After subscribing to a ReactiveProperty, the callback will be invoked immediately with the current value.
        // Use skip(1) to ignore the first value if needed.
        var subs = new CompositeDisposable();
        property.Subscribe(value => rangeControl.SetValue(double.CreateChecked(value))).AddTo(subs);
        rangeControl.OnValueChangedAsObservable()
            .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
        return subs;
    }

    public static CompositeDisposable BindValue<T>(this HSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        return BindValue((Godot.Range)slider, property);
    }
    
    public static CompositeDisposable BindValue<T>(this VSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        return BindValue((Godot.Range)slider, property);
    }
    
    public static CompositeDisposable BindValue<T>(this SpinBox spinBox, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        return BindValue((Godot.Range)spinBox, property);
    }
}