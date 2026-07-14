using Godot;

namespace Ciallo.Widget;

[Tool]
public partial class DockableSplitHandle : Control
{
    // Both tables mirror DockableLayoutSplit.SplitDirection's numeric order.
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

    // Drag ratios use the whole subtree rect, not the separator-width control rect.
    private Rect2 _parentRect;
    private bool _mouseHovering;
    private bool _dragging;

    public DockableLayoutSplit LayoutSplit { get; set; }
    public Vector2 FirstMinimumSize { get; set; }
    public Vector2 SecondMinimumSize { get; set; }

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Draw()
    {
        string themeClass = SplitThemeClass[(int)LayoutSplit.Direction];
        DrawStyleBox(
            GetThemeStylebox("split_bar_background", themeClass),
            new Rect2(Vector2.Zero, Size)
        );

        var icon = GetThemeIcon("grabber", themeClass);
        bool autohide = GetThemeConstant("autohide", themeClass) != 0;
        if (autohide && !_mouseHovering && !_dragging) return;

        DrawTexture(icon, (Size - icon.GetSize()) * 0.5f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            SetDragging(mouseButton.Pressed);
            if (mouseButton.DoubleClick)
                LayoutSplit.Percent = 0.5f;
            AcceptEvent();
        }
        else if (_dragging && @event is InputEventMouseMotion)
        {
            Vector2 mouseInParent = GetParentControl().GetLocalMousePosition();
            LayoutSplit.Percent = LayoutSplit.IsHorizontal()
                ? (mouseInParent.X - _parentRect.Position.X) / _parentRect.Size.X
                : (mouseInParent.Y - _parentRect.Position.Y) / _parentRect.Size.Y;
        }
    }

    // Why have to use this _Process extra guard: 
    // Without this, dragging cannot be released correctly when mouse is released.
    // Debugging on Godot 4.6.2/Windows ruled out focus loss, embedded-window input,
    // GUI drag, relayout/replacement, and early project input handling. We observed
    // Input report Left as released while neither _Input nor _GuiInput received the
    // release event, so guess this is engine-level dispatch gap while dragging.
    public override void _Process(double delta)
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
            SetDragging(false);
    }

    public override void _Notification(int what)
    {
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
    }

    public Vector2 GetLayoutMinimumSize()
    {
        int separation = GetSeparation();
        return LayoutSplit.IsHorizontal()
            ? new Vector2(FirstMinimumSize.X + separation + SecondMinimumSize.X, Mathf.Max(FirstMinimumSize.Y, SecondMinimumSize.Y))
            : new Vector2(Mathf.Max(FirstMinimumSize.X, SecondMinimumSize.X), FirstMinimumSize.Y + separation + SecondMinimumSize.Y);
    }

    public void SetSplitCursor(bool value)
    {
        MouseDefaultCursorShape = value ? SplitMouseCursorShape[(int)LayoutSplit.Direction] : CursorShape.Arrow;
    }

    private void SetDragging(bool value)
    {
        if (_dragging == value) return;

        _dragging = value;
        SetProcess(value);
        if (GetThemeConstant("autohide", SplitThemeClass[(int)LayoutSplit.Direction]) != 0)
            QueueRedraw();
    }

    public (Rect2 First, Rect2 Self, Rect2 Second) GetSplitRects(Rect2 rect)
    {
        _parentRect = rect;
        int separation = GetSeparation();
        Vector2 origin = rect.Position;
        float percent = LayoutSplit.Percent;

        if (LayoutSplit.IsHorizontal())
        {
            var sizes = CalculateAxisSizes(
                rect.Size.X,
                separation,
                percent,
                FirstMinimumSize.X,
                SecondMinimumSize.X
            );

            return (
                new Rect2(origin.X, origin.Y, sizes.First, rect.Size.Y),
                new Rect2(origin.X + sizes.First, origin.Y, sizes.Separation, rect.Size.Y),
                new Rect2(origin.X + sizes.First + sizes.Separation, origin.Y, sizes.Second, rect.Size.Y)
            );
        }

        var verticalSizes = CalculateAxisSizes(
            rect.Size.Y,
            separation,
            percent,
            FirstMinimumSize.Y,
            SecondMinimumSize.Y
        );

        return (
            new Rect2(origin.X, origin.Y, rect.Size.X, verticalSizes.First),
            new Rect2(origin.X, origin.Y + verticalSizes.First, rect.Size.X, verticalSizes.Separation),
            new Rect2(origin.X, origin.Y + verticalSizes.First + verticalSizes.Separation, rect.Size.X, verticalSizes.Second)
        );
    }

    private int GetSeparation()
    {
        string themeClass = SplitThemeClass[(int)LayoutSplit.Direction];
        int separation = GetThemeConstant("separation", themeClass);
        Vector2 grabberSize = GetThemeIcon("grabber", themeClass).GetSize();
        return Mathf.Max(separation, LayoutSplit.IsHorizontal() ? (int)grabberSize.X : (int)grabberSize.Y);
    }

    private static (float First, float Separation, float Second) CalculateAxisSizes(
        float axisSize,
        float separation,
        float percent,
        float firstMinimumSize,
        float secondMinimumSize)
    {
        axisSize = Mathf.Max(0, axisSize);
        separation = Mathf.Clamp(separation, 0, axisSize);
        float availableSize = axisSize - separation;
        float maximumFirstSize = availableSize - secondMinimumSize;

        if (firstMinimumSize > maximumFirstSize)
        {
            // The parent should honor DockableContainer's reported minimum size. Preserve both
            // leaf minima during a transient undersize instead of corrupting their layouts.
            return (firstMinimumSize, separation, secondMinimumSize);
        }

        float firstSize = Mathf.Clamp(
            // Percent denotes the separator center, consistent across either split direction.
            axisSize * percent - separation * 0.5f,
            firstMinimumSize,
            maximumFirstSize
        );

        return (firstSize, separation, availableSize - firstSize);
    }
}
