using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindLineEdit
{
    public static void BindString(this LineEdit lineEdit, ReactiveProperty<string> property, out CompositeDisposable subs)
    {
        subs = new();
        property.Subscribe(value =>
        {
            // `lineEdit.Text = value` makes the control reset its cursor. Must manually check to get correct input behavior.
            if(lineEdit.Text == value) return; 
            lineEdit.Text = value;
        }).AddTo(subs);
        lineEdit.OnTextChangedAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
    }

    public static void BindString(this LineEdit lineEdit, ReactiveProperty<string> property)
    {
        BindString(lineEdit, property, out var subs);
        subs.AddTo(lineEdit);
    }
}