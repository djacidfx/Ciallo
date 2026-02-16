using System;
using Godot;
using R3;

namespace Ciallo;

public static class BindCheckBox
{
    private static BaseButton BindBool(this BaseButton button, ReactiveProperty<bool> property, out CompositeDisposable subs)
    {
        if (!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        subs = new CompositeDisposable();
        property.Subscribe(value => button.ButtonPressed = value).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
        return button;
    }

    extension(CheckBox checkBox)
    {
        public CheckBox BindBool(ReactiveProperty<bool> property, out CompositeDisposable subs)
        {
            BindBool((BaseButton)checkBox, property, out subs);
            return checkBox;
        }
        public CheckBox BindBool(ReactiveProperty<bool> property)
        {
            BindBool(checkBox, property, out var sub);
            sub.AddTo(checkBox);
            return checkBox;
        }
        /// <summary>
        /// Bind a bitflag property to a CheckBox.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="mask">The bits to toggle on and off</param>
        /// <param name="subs"></param>
        /// <typeparam name="T">Enum with FlagsAttribute</typeparam>
        public CheckBox BindFlag<T>(ReactiveProperty<T> property, T mask, out CompositeDisposable subs) where T : Enum
        {
            subs = new CompositeDisposable();

            // reflect enum bits to checkbox pressed state
            property.Subscribe(value => checkBox.ButtonPressed = value.HasFlag(mask)).AddTo(subs);
            checkBox
                .OnToggledAsObservable()
                .Subscribe(pressed =>
                {
                    property.Value = pressed
                        ? (T)((dynamic)property.Value | (dynamic)mask)
                        : (T)((dynamic)property.Value & ~(dynamic)mask);
                })
                .AddTo(subs);
            return checkBox;
        }
        public CheckBox BindFlag<T>(ReactiveProperty<T> property, T mask) where T : Enum
        {
            BindFlag(checkBox, property, mask, out var sub);
            sub.AddTo(checkBox);
            return checkBox;
        }
    }
}