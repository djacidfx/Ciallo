using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool, GlobalClass]
public partial class DockableSplitHandle : Control
{
    private static readonly string[] SplitThemeClass =
    [
        "HSplitContainer",
        "VSplitContainer",
    ];

    private static readonly CursorShape[] SplitMouseCursorShape =
    [
        CursorShape.Hsplit,
        CursorShape.Vsplit,
    ];

    private Rect2 _parentRect;
    private bool _mouseHovering;
    private bool _dragging;

    public DockableLayoutSplit LayoutSplit { get; set; }
    public Vector2 FirstMinimumSize { get; set; }
    public Vector2 SecondMinimumSize { get; set; }

    public override void _Draw()
    {
        base._Draw();
        string themeClass = SplitThemeClass[(int)LayoutSplit.Direction];
        var icon = GetThemeIcon("grabber", themeClass);
        bool autohide = GetThemeConstant("autohide", themeClass) != 0;
        if (icon == null || autohide && !_mouseHovering) return;

        DrawTexture(icon, (Size - icon.GetSize()) * 0.5f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            _dragging = mouseButton.Pressed;
            if (mouseButton.DoubleClick)
                LayoutSplit.Percent = 0.5f;
        }
        else if (_dragging && @event is InputEventMouseMotion)
        {
            Vector2 mouseInParent = GetParentControl().GetLocalMousePosition();
            LayoutSplit.Percent = LayoutSplit.IsHorizontal()
                ? (mouseInParent.X - _parentRect.Position.X) / _parentRect.Size.X
                : (mouseInParent.Y - _parentRect.Position.Y) / _parentRect.Size.Y;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationMouseEnter)
        {
            _mouseHovering = true;
            SetSplitCursor(true);
            if (GetThemeConstant("autohide", SplitThemeClass[(int)LayoutSplit.Direction]) != 0)
                QueueRedraw();
        }
        else if (what == NotificationMouseExit)
        {
            _mouseHovering = false;
            SetSplitCursor(false);
            if (GetThemeConstant("autohide", SplitThemeClass[(int)LayoutSplit.Direction]) != 0)
                QueueRedraw();
        }
        else if (what == NotificationFocusExit)
        {
            _dragging = false;
        }
    }

    public Vector2 GetLayoutMinimumSize()
    {
        int separation = GetThemeConstant("separation", SplitThemeClass[(int)LayoutSplit.Direction]);
        return LayoutSplit.IsHorizontal()
            ? new Vector2(FirstMinimumSize.X + separation + SecondMinimumSize.X, Mathf.Max(FirstMinimumSize.Y, SecondMinimumSize.Y))
            : new Vector2(Mathf.Max(FirstMinimumSize.X, SecondMinimumSize.X), FirstMinimumSize.Y + separation + SecondMinimumSize.Y);
    }

    public void SetSplitCursor(bool value)
    {
        MouseDefaultCursorShape = value ? SplitMouseCursorShape[(int)LayoutSplit.Direction] : CursorShape.Arrow;
    }

    public (Rect2 First, Rect2 Self, Rect2 Second) GetSplitRects(Rect2 rect)
    {
        _parentRect = rect;
        int separation = GetThemeConstant("separation", SplitThemeClass[(int)LayoutSplit.Direction]);
        Vector2 origin = rect.Position;
        float percent = LayoutSplit.Percent;

        if (LayoutSplit.IsHorizontal())
        {
            float splitOffset = Mathf.Clamp(
                rect.Size.X * percent - separation * 0.5f,
                FirstMinimumSize.X,
                rect.Size.X - SecondMinimumSize.X - separation
            );
            float secondWidth = rect.Size.X - splitOffset - separation;

            return (
                new Rect2(origin.X, origin.Y, splitOffset, rect.Size.Y),
                new Rect2(origin.X + splitOffset, origin.Y, separation, rect.Size.Y),
                new Rect2(origin.X + splitOffset + separation, origin.Y, secondWidth, rect.Size.Y)
            );
        }

        float verticalSplitOffset = Mathf.Clamp(
            rect.Size.Y * percent - separation * 0.5f,
            FirstMinimumSize.Y,
            rect.Size.Y - SecondMinimumSize.Y - separation
        );
        float secondHeight = rect.Size.Y - verticalSplitOffset - separation;

        return (
            new Rect2(origin.X, origin.Y, rect.Size.X, verticalSplitOffset),
            new Rect2(origin.X, origin.Y + verticalSplitOffset, rect.Size.X, separation),
            new Rect2(origin.X, origin.Y + verticalSplitOffset + separation, rect.Size.X, secondHeight)
        );
    }
}
