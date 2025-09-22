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

    public static CompositeDisposable BindColor(this ColorPickerButton button, ReactiveProperty<Color> property)
    {
        //// Direct get picker binding make the preview color of the button not update correctly
        // return BindColor(button.GetPicker(), property);
        CompositeDisposable subs = new();
        property.Subscribe(c => button.Color = c).AddTo(subs);
        button.SignalAsObservable<Color>(ColorPickerButton.SignalName.ColorChanged)
            .Subscribe(c => property.Value = c).AddTo(subs);
        return subs;
    }
    
    public static CompositeDisposable ReactiveBindColor(this ColorPickerButton button, ReactivePropertyView<ReactiveProperty<Color>> view)
    {
        var resultSubs = new CompositeDisposable();
        CompositeDisposable curSubs = null;
        view.Subscribe(property =>
        {
            if (property == null)
            {
                curSubs?.Dispose();
                curSubs = null;
            }
            if (property != null)
            {
                curSubs?.Dispose();
                curSubs = button.BindColor(property);
            }
        }).AddTo(resultSubs);

        return resultSubs;
    }
}