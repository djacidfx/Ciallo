using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindVector2Edit
{
    public static CompositeDisposable BindVector2(this Vector2Edit control, ReactiveProperty<Vector2> property)
    {
        CompositeDisposable subs = new();
        property.Subscribe(v => control.Value = v).AddTo(subs);
        control.SignalAsObservable<Vector2>(Vector2Edit.SignalName.ValueChanged)
            .Subscribe(v => property.Value = v)
            .AddTo(subs);
        return subs;
    }
}