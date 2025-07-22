using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindRangeExtension
{
    public static void BindValue<T>(Godot.Range rangeControl, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        // Note: After subscribing to a ReactiveProperty, the callback will be invoked immediately with the current value.
        // Use skip(1) to ignore the first value.
        var subscription = property.Subscribe(value => rangeControl.SetValueNoSignal(Convert.ToDouble(value)));
        rangeControl.ValueChanged += value => property.Value = (T)Convert.ChangeType(value, typeof(T));

        if (rangeControl.IsInsideTree())
        {
            subscription.AddTo(rangeControl);
        }
        else
        {
            rangeControl.SignalAsObservable(Node.SignalName.TreeEntered)
                .Take(1)
                .Subscribe(_ => subscription.AddTo(rangeControl));
        }
    }

    public static void BindValue<T>(this HSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindValue((Godot.Range)slider, property);
    }
    
    public static void BindValue<T>(this VSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindValue((Godot.Range)slider, property);
    }
    
    public static void BindValue<T>(this SpinBox spinBox, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindValue((Godot.Range)spinBox, property);
    }
}