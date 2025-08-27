using System;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindToggleButtonExtension
{
    private static CompositeDisposable BindBool(this BaseButton button, ReactiveProperty<bool> property)
    {
        if(!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        var subs = new CompositeDisposable();
        property.Subscribe(value => button.ButtonPressed = value).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
        return subs;
    }
    
    public static CompositeDisposable BindBool(this CheckBox checkBox, ReactiveProperty<bool> property)
    {
        return BindBool((BaseButton)checkBox, property);
    }
    
    public static CompositeDisposable BindBool(this Button button, ReactiveProperty<bool> property)
    {
        return BindBool((BaseButton)button, property);
    }
}