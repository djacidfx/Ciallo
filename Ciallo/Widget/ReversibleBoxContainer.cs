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
            QueueSort();
        }
    } = true;

    public override void _Notification(int what)
    {
        // Always call base so BoxContainer handles sizing, theming, and other notifications.
        base._Notification(what);
        if (what == NotificationSortChildren) _resort();
    }

    // Virtual so subclasses can override layout while still triggering it via _Notification.
    protected virtual void _resort()
    {
        // When not reversing, BoxContainer's base layout (called above) already handled it.
        if (!ReverseOrder) return;
        if (GetChildCount() == 0) return;

        int separation = GetThemeConstant("separation");
        float offset = 0;

        var controls = new List<Control>();
        foreach (Node child in GetChildren())
        {
            if (child is Control c && c.Visible && !c.IsSetAsTopLevel())
                controls.Add(c);
        }

        // Base BoxContainer already set sizes; we only re-order positions.
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