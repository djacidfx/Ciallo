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
            // Note: Setting button color in code donesn't trigger ColorChanged signal, unlike Button and Range.
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
            BindColor(button, property, out var subs);
            subs.AddTo(button);
            return button;
        }
    }
}