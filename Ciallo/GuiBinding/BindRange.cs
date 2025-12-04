using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiBinding;

public static class BindRange
{
    private static void BindNumber<T>(Range rangeControl,
        [NotNull] ReactiveProperty<T> property,
        out CompositeDisposable subs) where T : INumber<T>
    {
        // Note: After subscribing to a ReactiveProperty, the callback will be invoked immediately with the current value.
        // Use skip(1) to ignore the first value if needed.
        subs = new();
        property.Subscribe(value => rangeControl.SetValue(double.CreateChecked(value))).AddTo(subs);
        rangeControl.OnValueChangedAsObservable()
            .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
    }

    public static void BindNumber<T>(this Range rangeControl, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber(rangeControl, property, out var subs);
        subs.AddTo(rangeControl);
    }

    public static void BindNumber<T>(this HSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)slider, property);
    }

    public static void BindNumber<T>(this VSlider slider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)slider, property);
    }

    public static void BindNumber<T>(this SpinBox spinBox, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)spinBox, property);
    }

    public static void BindNumber<T>(this SpinSlider spinSlider,
        [NotNull] ReactiveProperty<T> property,
        out CompositeDisposable subs) where T : INumber<T>
    {
        subs = new();
        property.Subscribe(value => spinSlider.Value = double.CreateChecked(value)).AddTo(subs);
        spinSlider.SignalAsObservable<float>(SpinSlider.SignalName.ValueChanged)
            .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
    }

    public static void BindNumber<T>(this SpinSlider spinSlider, [NotNull] ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber(spinSlider, property, out var subs);
        subs.AddTo(spinSlider);
    }

    public static void ReactiveBindNumber<T>(this SpinSlider spinSlider,
        ReadOnlyReactiveProperty<ReactiveProperty<T>> view,
        out CompositeDisposable subs) where T : INumber<T>
    {
        subs = new();
        CompositeDisposable curSub = null;
        var disposable = subs;
        view.Subscribe(property =>
        {
            curSub?.Dispose();
            if (property == null) curSub = null;
            else
            {
                spinSlider.BindNumber(property, out curSub);
                curSub.AddTo(disposable);
            }
        }).AddTo(subs);
    }

    /// <summary>
    /// Binds a SpinSlider dynamically to a ReactiveProperty provided by the ReadOnlyReactiveProperty.
    /// </summary>
    public static void ReactiveBindNumber<T>(this SpinSlider spinSlider,
        ReadOnlyReactiveProperty<ReactiveProperty<T>> view) where T : INumber<T>
    {
        ReactiveBindNumber(spinSlider, view, out var subs);
        subs.AddTo(spinSlider);
    }
}