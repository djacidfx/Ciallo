using System;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindToggleButton
{
    private static CompositeDisposable BindValue(this BaseButton button, ReactiveProperty<bool> property)
    {
        if(!button.ToggleMode) throw new ArgumentException("Button must be in toggle mode", nameof(button));
        var subs = new CompositeDisposable();
        property.Subscribe(value => button.ButtonPressed = value).AddTo(subs);
        button.OnToggledAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
        return subs;
    }
    
    public static CompositeDisposable BindValue(this CheckBox checkBox, ReactiveProperty<bool> property)
    {
        return BindValue((BaseButton)checkBox, property);
    }
    
    public static CompositeDisposable BindValue(this Button button, ReactiveProperty<bool> property)
    {
        return BindValue((BaseButton)button, property);
    }
}