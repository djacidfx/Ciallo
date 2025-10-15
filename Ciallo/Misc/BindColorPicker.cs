using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindColorPicker
{
    public static CompositeDisposable BindColor(this ColorPicker picker, ReactiveProperty<Color> property)
    {
        CompositeDisposable subs = new();
        property.Subscribe(c => picker.Color = c).AddTo(subs);
        picker.SignalAsObservable<Color>(ColorPicker.SignalName.ColorChanged)
            .Subscribe(c => property.Value = c)
            .AddTo(subs);
        return subs;
    }

    public static void BindColor(this ColorPickerButton button, ReactiveProperty<Color> property, out CompositeDisposable subs)
    {
        //// Direct get picker binding make the preview color of the button not update correctly
        // return BindColor(button.GetPicker(), property);
        subs = new();
        property.Subscribe(c => button.Color = c).AddTo(subs);
        button.SignalAsObservable<Color>(ColorPickerButton.SignalName.ColorChanged)
            .Subscribe(c => property.Value = c).AddTo(subs);
    }

    public static void BindColor(this ColorPickerButton button, ReactiveProperty<Color> property)
    {
        BindColor(button, property, out var subs);
        subs.AddTo(button);
    }

    public static void ReactiveBindColor(this ColorPickerButton button, ReadOnlyReactiveProperty<ReactiveProperty<Color>> view)
    {
        var subs = new CompositeDisposable();
        CompositeDisposable curSub = null;
        view.Subscribe(property =>
        {
            curSub?.Dispose();
            if (property == null) curSub = null;
            else
            {
                button.BindColor(property, out curSub);
                curSub.AddTo(subs);
            }
        }).AddTo(subs);

        subs.AddTo(button);
    }
}