using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.GuiBinding;

public static class BindVector2Edit
{
    public static void BindVector2(this Vector2Edit control, ReactiveProperty<Vector2> property, out CompositeDisposable subs)
    {
        subs = new();
        property.Subscribe(v => control.Value = v).AddTo(subs);
        control.SignalAsObservable<Vector2>(Vector2Edit.SignalName.ValueChanged)
            .Subscribe(v => property.Value = v)
            .AddTo(subs);
    }
    public static void BindVector2(this Vector2Edit control, ReactiveProperty<Vector2> property)
    {
        BindVector2(control, property, out var subs);
        subs.AddTo(control);
    }
}