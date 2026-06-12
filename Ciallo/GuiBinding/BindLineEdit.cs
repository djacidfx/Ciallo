using Godot;
using R3;

namespace Ciallo;

public static class BindLineEdit
{
    extension(LineEdit lineEdit)
    {
        public LineEdit BindString(ReactiveProperty<string> property, CompositeDisposable subs)
        {
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
        public LineEdit BindString(ReactiveProperty<string> property)
        {
            var subs = new CompositeDisposable();
            BindString(lineEdit, property, subs);
            subs.AddTo(lineEdit);
            return lineEdit;
        }
        public LineEdit SubmitOnFocusExit()
        {
            lineEdit.FocusExited += () => lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
            return lineEdit;
        }
    }
}