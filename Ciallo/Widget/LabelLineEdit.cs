using Godot;
using System;

namespace Ciallo.Widget;

/// <summary>
/// <para>Act as a label. Being editable after double-clicked. Lose focus return to the label.</para>
/// </summary>
[Tool, GlobalClass]
public partial class LabelLineEdit : LineEdit
{
    public override void _Ready()
    {
        Editable = false;
        MiddleMousePasteEnabled = false;
        MouseDefaultCursorShape = CursorShape.Arrow;
        
        var styleBox = GetThemeStylebox("normal") ?? throw new InvalidOperationException("Theme stylebox 'normal' not found.");
        var fontColor = GetThemeColor("font_color");
        AddThemeStyleboxOverride("read_only", styleBox);
        AddThemeColorOverride("font_uneditable_color", fontColor);
        
        Connect(LineEdit.SignalName.TextSubmitted, new Callable(this, nameof(OnTextSubmitted)));
        Connect(Control.SignalName.FocusExited, new Callable(this, nameof(OnFocusExited)));
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: true } && !Editable)
        {
            Editable = true;
            MouseDefaultCursorShape = CursorShape.Ibeam;
        }
        Edit();
    }

    public void OnTextSubmitted(string newText)
    {
        Editable = false;
        Unedit();
        MouseDefaultCursorShape = CursorShape.Arrow;
    }
    
    public void OnFocusExited()
    {
        if (!Editable) return; // IsEditing is false here.
        Editable = false;
        Unedit();
        MouseDefaultCursorShape = CursorShape.Arrow;
    }
}
