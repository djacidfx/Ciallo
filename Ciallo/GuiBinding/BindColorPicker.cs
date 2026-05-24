using Godot;
using R3;

namespace Ciallo;

public static class BindColorPicker
{
    extension(ColorPickerButton button)
    {
        public ColorPickerButton BindColor(ReactiveProperty<Color> property, CompositeDisposable subs)
        {
            // Note: Setting button color in code doesn't trigger ColorChanged signal, unlike Button and Range.
            property.Subscribe(c =>
            {
                // Need to check or a bouncing will happen.
                if (!button.Color.IsEqualApprox(c))
                    button.Color = c;
            }).AddTo(subs);
            button.SignalAsObservable<Color>(ColorPickerButton.SignalName.ColorChanged)
                .Subscribe(c => property.Value = c)
                .AddTo(subs);
            return button;
        }
        public ColorPickerButton BindColor(ReactiveProperty<Color> property)
        {
            var subs = new CompositeDisposable();
            BindColor(button, property, subs);
            subs.AddTo(button);
            return button;
        }
    }
}