using System;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindToggleButton
{
    private static void BindBool(this BaseButton button, ReactiveProperty<bool> property, out CompositeDisposable subs)
    {
        if (!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        subs = new CompositeDisposable();
        property.Subscribe(value => button.ButtonPressed = value).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
    }

    public static void BindBool(this CheckBox checkBox, ReactiveProperty<bool> property, out CompositeDisposable subs)
    {
        BindBool((BaseButton)checkBox, property, out subs);
    }

    public static void BindBool(this CheckBox checkBox, ReactiveProperty<bool> property)
    {
        BindBool(checkBox, property, out var sub);
        sub.AddTo(checkBox);
    }

    /// <summary>
    /// Bind a bitflag property to a CheckBox.
    /// </summary>
    /// <param name="checkBox"></param>
    /// <param name="property"></param>
    /// <param name="mask">The bits to toggle on and off</param>
    /// <param name="subs"></param>
    /// <typeparam name="T">Enum with FlagsAttribute</typeparam>
    public static void BindFlag<T>(this CheckBox checkBox, ReactiveProperty<T> property, T mask, out CompositeDisposable subs) where T : Enum
    {
        subs = new CompositeDisposable();

        // reflect enum bits to checkbox pressed state
        property.Subscribe(value => checkBox.ButtonPressed = value.HasFlag(mask)).AddTo(subs);

        // update enum bits when checkbox toggled (use dynamic to keep it generic and avoid integer conversions)
        checkBox
            .OnToggledAsObservable()
            .Subscribe(pressed =>
            {
                property.Value = pressed
                    ? (T)((dynamic)property.Value | (dynamic)mask)
                    : (T)((dynamic)property.Value & ~(dynamic)mask);
            })
            .AddTo(subs);
    }

    public static void BindFlag<T>(this CheckBox checkBox, ReactiveProperty<T> property, T mask) where T : Enum
    {
        BindFlag(checkBox, property, mask, out var sub);
        sub.AddTo(checkBox);
    }
}