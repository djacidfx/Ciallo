using Godot;
using R3;

namespace Ciallo;

public static class BindLineEdit
{
    public static LineEdit BindString(this LineEdit lineEdit, ReactiveProperty<string> property, out CompositeDisposable subs)
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
        return lineEdit;
    }

    public static LineEdit BindString(this LineEdit lineEdit, ReactiveProperty<string> property)
    {
        BindString(lineEdit, property, out var subs);
        subs.AddTo(lineEdit);
        return lineEdit;
    }

    public static LineEdit SubmitOnFocusExit(this LineEdit lineEdit)
    {
        lineEdit.FocusExited += () => lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
        return lineEdit;
    }
}