using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindLineEdit
{
    public static CompositeDisposable BindString(this LineEdit lineEdit, ReactiveProperty<string> property)
    {
        var subs = new CompositeDisposable();
        property.Subscribe(value =>
        {
            // `lineEdit.Text = value` makes the control reset its cursor. Must manually check to get correct input behavior.
            if(lineEdit.Text == value) return; 
            lineEdit.Text = value;
        }).AddTo(subs);
        lineEdit.OnTextChangedAsObservable()
            .Subscribe(value => property.Value = value).AddTo(subs);
        return subs;
    }
}