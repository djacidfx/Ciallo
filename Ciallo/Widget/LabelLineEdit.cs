using Godot;

namespace Ciallo.Widget;

/// <summary>
/// <para>Act as a label. Being editable after double-clicked. Lose focus return to the label.</para>
/// </summary>
[GlobalClass]
public partial class LabelLineEdit : LineEdit
{
    public CursorShape DefaultCursorShape;
    private string _textBeforeEdit = "";

    public LabelLineEdit()
    {
        ContextMenuEnabled = false;
    }

    public override void _Ready()
    {
        Editable = false;
        SelectingEnabled = false;
        MiddleMousePasteEnabled = false;
        DefaultCursorShape = MouseDefaultCursorShape;

        Connect(LineEdit.SignalName.TextSubmitted, new Callable(this, nameof(OnTextSubmitted)));
        Connect(Control.SignalName.FocusExited, new Callable(this, nameof(OnFocusExited)));

        MouseDefaultCursorShape = DefaultCursorShape;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: true } && !Editable)
        {
            _textBeforeEdit = Text;
            Editable = true;
            SelectingEnabled = true;
            MouseDefaultCursorShape = CursorShape.Ibeam;
            Edit();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (Editable && AppHotkeys.UiCancel.IsPressedBy(e))
        {
            Text = _textBeforeEdit;
            FinishEdit();
            GetViewport().SetInputAsHandled();
        }
    }

    public void OnTextSubmitted(string newText)
    {
        FinishEdit();
    }

    public void OnFocusExited()
    {
        if (!Editable) return; // IsEditing is false here.
        FinishEdit();
    }

    private void FinishEdit()
    {
        Editable = false;
        SelectingEnabled = false;
        Unedit();
        MouseDefaultCursorShape = DefaultCursorShape;
    }
}
