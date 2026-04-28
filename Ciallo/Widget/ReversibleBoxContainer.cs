using System.Collections.Generic;
using Godot;

[Tool, GlobalClass]
public partial class ReversibleBoxContainer : BoxContainer
{
    [Export] public bool ReverseOrder
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            _Notification((int)NotificationSortChildren);
        }
    } = true;

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren) _resort();
    }

    private void _resort()
    {
        if (!ReverseOrder) return;
        if (GetChildCount() == 0) return;

        int separation = GetThemeConstant("separation");
        float offset = separation;

        var controls = new List<Control>();
        foreach (Node child in GetChildren())
        {
            if (child is Control c && c.Visible && !c.IsSetAsTopLevel())
            {
                controls.Add(c);
            }
        }

        for (int i = controls.Count - 1; i >= 0; i--)
        {
            var control = controls[i];
            if (Vertical)
            {
                control.Position = new Vector2(control.Position.X, offset);
                offset += control.Size.Y + separation;
            }
            else
            {
                control.Position = new Vector2(offset, control.Position.Y);
                offset += control.Size.X + separation;
            }
        }
    }
}