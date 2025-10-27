using System.Collections.Generic;
using Godot;

[Tool, GlobalClass]
public partial class ReversedVBoxContainer : VBoxContainer
{
    private bool _reverseOrder = true;
    [Export] public bool ReverseOrder
    {
        get => _reverseOrder;
        set
        {
            if (value == _reverseOrder) return;
            _reverseOrder = value;
            _Notification((int)NotificationSortChildren);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren) _resort();
    }

    private void _resort()
    {
        // Gen by gpt-5
        if (!ReverseOrder) return;

        // Ensure the main row exists under this node.
        if (GetChildCount() == 0) return;

        // Use the same separation as BoxContainer/VBoxContainer.
        int separation = GetThemeConstant("separation", "BoxContainer");

        // Start laying out children below the main row, but in reverse order.
        float y = separation;

        // Collect all direct child Controls respecting visibility and top-level status.
        var others = new List<Control>();
        foreach (Node child in GetChildren())
        {
            if (child is Control c && c.Visible && !c.IsSetAsTopLevel())
            {
                others.Add(c);
            }
        }

        // Layout children in reverse order.
        for (int i = others.Count - 1; i >= 0; i--)
        {
            var c = others[i];
            c.Position = new Vector2(c.Position.X, y);
            y += c.Size.Y + separation;
        }
    }
}