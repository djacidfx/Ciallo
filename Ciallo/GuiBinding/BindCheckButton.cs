using System;
using Godot;
using R3;

namespace Ciallo;

public static class BindCheckButton
{
    public static void BindBool(this BaseButton button, ReactiveProperty<bool> property, CompositeDisposable subs)
    {
        if (!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        property.Subscribe(button.SetPressedNoSignal).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
    }

    extension(CheckButton checkButton)
    {
        public CheckButton BindBool(ReactiveProperty<bool> property, CompositeDisposable subs)
        {
            BindBool((BaseButton)checkButton, property, subs);
            return checkButton;
        }
        public CheckButton BindBool(ReactiveProperty<bool> property)
        {
            var subs = new CompositeDisposable();
            BindBool(checkButton, property, subs);
            subs.AddTo(checkButton);
            return checkButton;
        }
        /// <summary>
        /// Bind a bitflag property to a CheckButton.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="mask">The bits to toggle on and off</param>
        /// <param name="subs"></param>
        /// <typeparam name="T">Enum with FlagsAttribute</typeparam>
        public CheckButton BindFlag<T>(ReactiveProperty<T> property, T mask, CompositeDisposable subs) where T : Enum
        {
            // reflect enum bits to checkbutton pressed state
            property.Subscribe(value => checkButton.SetPressedNoSignal(value.HasFlag(mask))).AddTo(subs);
            checkButton
                .OnToggledAsObservable()
                .Subscribe(pressed =>
                {
                    property.Value = pressed
                        ? (T)((dynamic)property.Value | (dynamic)mask)
                        : (T)((dynamic)property.Value & ~(dynamic)mask);
                })
                .AddTo(subs);
            return checkButton;
        }
        public CheckButton BindFlag<T>(ReactiveProperty<T> property, T mask) where T : Enum
        {
            var subs = new CompositeDisposable();
            checkButton.BindFlag(property, mask, subs);
            subs.AddTo(checkButton);
            return checkButton;
        }
    }
}