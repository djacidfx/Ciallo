using System.Numerics;
using Ciallo.Widget;
using Godot;
using R3;
using Range = Godot.Range;

namespace Ciallo;

public static class BindRange
{
    private static Range BindNumber<T>(Range rangeControl,
        ReactiveProperty<T> property,
        out CompositeDisposable subs) where T : INumber<T>
    {
        // Note: After subscribing to a ReactiveProperty, the callback will be invoked immediately with the current value.
        // Use skip(1) to ignore the first value if needed.
        subs = new();
        property.Subscribe(value => rangeControl.SetValue(double.CreateChecked(value))).AddTo(subs);
        rangeControl.OnValueChangedAsObservable()
            .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
        return rangeControl;
    }

    public static Range BindNumber<T>(this Range rangeControl, ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber(rangeControl, property, out var subs);
        subs.AddTo(rangeControl);
        return rangeControl;
    }

    public static HSlider BindNumber<T>(this HSlider slider, ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)slider, property, out var subs);
        subs.AddTo(slider);
        return slider;
    }

    public static VSlider BindNumber<T>(this VSlider slider, ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)slider, property, out var subs);
        subs.AddTo(slider);
        return slider;
    }

    public static SpinBox BindNumber<T>(this SpinBox spinBox, ReactiveProperty<T> property) where T : INumber<T>
    {
        BindNumber((Range)spinBox, property, out var subs);
        subs.AddTo(spinBox);
        return spinBox;
    }

    extension(SpinSlider spinSlider)
    {
        public SpinSlider BindNumber<T>(ReactiveProperty<T> property,
            out CompositeDisposable subs) where T : INumber<T>
        {
            subs = new();
            property.Subscribe(value => spinSlider.Value = double.CreateChecked<T>(value)).AddTo(subs);
            spinSlider.SignalAsObservable<float>(SpinSlider.SignalName.ValueChanged)
                .Subscribe(value => property.Value = T.CreateChecked(value)).AddTo(subs);
            return spinSlider;
        }
        public SpinSlider BindNumber<T>(ReactiveProperty<T> property) where T : INumber<T>
        {
            BindNumber(spinSlider, property, out var subs);
            subs.AddTo(spinSlider);
            return spinSlider;
        }
        public SpinSlider ReactiveBindNumber<T>(ReadOnlyReactiveProperty<ReactiveProperty<T>> view,
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
            return spinSlider;
        }
        /// <summary>
        /// Binds a SpinSlider dynamically to a ReactiveProperty provided by the ReadOnlyReactiveProperty.
        /// </summary>
        public SpinSlider ReactiveBindNumber<T>(ReadOnlyReactiveProperty<ReactiveProperty<T>> view) where T : INumber<T>
        {
            ReactiveBindNumber(spinSlider, view, out var subs);
            subs.AddTo(spinSlider);
            return spinSlider;
        }
    }
}