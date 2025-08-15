using Godot;
using System;

namespace Ciallo.Widget;

/// <summary>
/// <para>Act as a label. Being editable after double-clicked. Lose focus return to the label.</para>
/// </summary>
[GlobalClass]
public partial class LabelLineEdit : LineEdit
{
    public CursorShape DefaultCursorShape;
    
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
            Editable = true;
            SelectingEnabled = true;
            MouseDefaultCursorShape = CursorShape.Ibeam;
        }
        Edit();
    }

    public void OnTextSubmitted(string newText)
    {
        Editable = false;
        SelectingEnabled = false;
        Unedit();
        MouseDefaultCursorShape = DefaultCursorShape;
    }
    
    public void OnFocusExited()
    {
        if (!Editable) return; // IsEditing is false here.
        Editable = false;
        SelectingEnabled = false;
        Unedit();
        MouseDefaultCursorShape = DefaultCursorShape;
    }
}
