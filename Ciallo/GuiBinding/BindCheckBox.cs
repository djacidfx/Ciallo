using System;
using Godot;
using R3;

namespace Ciallo;

public static class BindCheckBox
{
    private static void BindBool(this BaseButton button, ReactiveProperty<bool> property, CompositeDisposable subs)
    {
        if (!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        property.Subscribe(button.SetPressedNoSignal).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
    }

    extension(CheckBox checkBox)
    {
        public CheckBox BindBool(ReactiveProperty<bool> property, CompositeDisposable subs)
        {
            BindBool((BaseButton)checkBox, property, subs);
            return checkBox;
        }
        public CheckBox BindBool(ReactiveProperty<bool> property)
        {
            var subs = new CompositeDisposable();
            BindBool(checkBox, property, subs);
            subs.AddTo(checkBox);
            return checkBox;
        }
        /// <summary>
        /// Bind a bitflag property to a CheckBox.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="mask">The bits to toggle on and off</param>
        /// <param name="subs"></param>
        /// <typeparam name="T">Enum with FlagsAttribute</typeparam>
        public CheckBox BindFlag<T>(ReactiveProperty<T> property, T mask, CompositeDisposable subs) where T : Enum
        {
            // reflect enum bits to checkbox pressed state
            property.Subscribe(value => checkBox.SetPressedNoSignal(value.HasFlag(mask))).AddTo(subs);
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
            var subs = new CompositeDisposable();
            checkBox.BindFlag(property, mask, subs);
            subs.AddTo(checkBox);
            return checkBox;
        }
    }
}