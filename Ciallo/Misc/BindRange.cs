using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ciallo.Widget;
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
    
    public static CompositeDisposable BindValue<T>(this SpinSlider spinSlider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        var subs = new CompositeDisposable();
        property.Subscribe(value => spinSlider.Value = double.CreateChecked(value)).AddTo(subs);
        spinSlider.SignalAsObservable<float>(SpinSlider.SignalName.ValueChanged)
            .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
        return subs;
    }

    public static CompositeDisposable ReactiveBindValue<T>(this SpinSlider spinSlider,
        ReactivePropertyView<ReactiveProperty<T>> view) where T : INumber<T>
    {
        var resultSubs = new CompositeDisposable();
        CompositeDisposable curSubs = null;
        view.Subscribe(property =>
        {
            if (property == null)
            {
                curSubs?.Dispose();
                curSubs = null;
            }
            if (property != null)
            {
                curSubs?.Dispose();
                curSubs = spinSlider.BindValue(property);
            }
        }).AddTo(resultSubs);

        return resultSubs;
    }
}