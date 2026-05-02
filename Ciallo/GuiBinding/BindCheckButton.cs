using System;
using Godot;
using R3;

namespace Ciallo;

public static class BindCheckButton
{
    public static void BindBool(this BaseButton button, ReactiveProperty<bool> property, out CompositeDisposable subs)
    {
        if (!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        subs = new CompositeDisposable();
        property.Subscribe(button.SetPressedNoSignal).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
    }

    extension(CheckButton checkButton)
    {
        public CheckButton BindBool(ReactiveProperty<bool> property, out CompositeDisposable subs)
        {
            BindBool((BaseButton)checkButton, property, out subs);
            return checkButton;
        }
        public CheckButton BindBool(ReactiveProperty<bool> property)
        {
            BindBool(checkButton, property, out var sub);
            sub.AddTo(checkButton);
            return checkButton;
        }
        /// <summary>
        /// Bind a bitflag property to a CheckButton.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="mask">The bits to toggle on and off</param>
        /// <param name="subs"></param>
        /// <typeparam name="T">Enum with FlagsAttribute</typeparam>
        public CheckButton BindFlag<T>(ReactiveProperty<T> property, T mask, out CompositeDisposable subs) where T : Enum
        {
            subs = new CompositeDisposable();

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
            checkButton.BindFlag(property, mask, out var sub);
            sub.AddTo(checkButton);
            return checkButton;
        }
    }
}