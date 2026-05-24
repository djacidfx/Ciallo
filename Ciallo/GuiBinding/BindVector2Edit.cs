using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo;

public static class BindVector2Edit
{
    extension(Vector2Edit control)
    {
        public Vector2Edit BindVector2(ReactiveProperty<Vector2> property, CompositeDisposable subs)
        {
            property.Subscribe(v => control.Value = v).AddTo(subs);
            control.SignalAsObservable<Vector2>(Vector2Edit.SignalName.ValueChanged)
                .Subscribe(v => property.Value = v)
                .AddTo(subs);
            return control;
        }

        public Vector2Edit BindVector2(ReactiveProperty<Vector2> property)
        {
            var subs = new CompositeDisposable();
            BindVector2(control, property, subs);
            subs.AddTo(control);
            return control;
        }
    }
}