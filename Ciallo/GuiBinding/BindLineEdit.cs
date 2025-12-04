using Godot;
using R3;

namespace Ciallo.GuiBinding;

public static class BindLineEdit
{
    public static void BindString(this LineEdit lineEdit, ReactiveProperty<string> property, out CompositeDisposable subs)
    {
        subs = new();
        property.Subscribe(value =>
        {
            // `lineEdit.Text = value` makes the control reset its cursor. Must manually check to get correct input behavior.
            if (lineEdit.Text == value) return;
            lineEdit.Text = value;
        }).AddTo(subs);
        lineEdit.OnTextSubmittedAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
        lineEdit.SubmitOnFocusExit();
    }

    public static void BindString(this LineEdit lineEdit, ReactiveProperty<string> property)
    {
        BindString(lineEdit, property, out var subs);
        subs.AddTo(lineEdit);
    }

    public static void SubmitOnFocusExit(this LineEdit lineEdit)
    {
        lineEdit.FocusExited += () => lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
    }
}