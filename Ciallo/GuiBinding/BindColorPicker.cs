using Godot;
using R3;

namespace Ciallo;

public static class BindColorPicker
{
    extension(ColorPickerButton button)
    {
        public ColorPickerButton BindColor(ReactiveProperty<Color> property, out CompositeDisposable subs)
        {
            //// Direct get picker binding make the preview color of the button not update correctly
            // return BindColor(button.GetPicker(), property);
            subs = new();
            property.Subscribe(c => button.Color = c).AddTo(subs);
            button.SignalAsObservable<Color>(ColorPickerButton.SignalName.ColorChanged)
                .Subscribe(c => property.Value = c).AddTo(subs);
            return button;
        }
        public ColorPickerButton BindColor(ReactiveProperty<Color> property)
        {
            BindColor(button, property, out var subs);
            subs.AddTo(button);
            return button;
        }
        public ColorPickerButton ReactiveBindColor(ReadOnlyReactiveProperty<ReactiveProperty<Color>> view)
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
            return button;
        }
    }
}