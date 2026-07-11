using Godot;

namespace Ciallo.Widget;

[Tool]
public partial class DockableDragNDropPanel : Control
{
    private const int DrawNothing = -1;
    private const int DrawCentered = -2;

    private int _drawMargin = DrawNothing;
    private bool _shouldSplit;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationMouseExit)
        {
            _drawMargin = DrawNothing;
            QueueRedraw();
        }
        else if (what == NotificationMouseEnter && !_shouldSplit)
        {
            _drawMargin = DrawCentered;
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);
        if (_shouldSplit && @event is InputEventMouseMotion motion)
        {
            _drawMargin = FindHoverMargin(motion.Position);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        base._Draw();
        Rect2 rect;
        switch (_drawMargin)
        {
            case DrawNothing:
                return;
            case DrawCentered:
                rect = new Rect2(Vector2.Zero, Size);
                break;
            case DockableLayout.MarginLeft:
                rect = new Rect2(0, 0, Size.X * 0.5f, Size.Y);
                break;
            case DockableLayout.MarginTop:
                rect = new Rect2(0, 0, Size.X, Size.Y * 0.5f);
                break;
            case DockableLayout.MarginRight:
                float halfWidth = Size.X * 0.5f;
                rect = new Rect2(halfWidth, 0, halfWidth, Size.Y);
                break;
            case DockableLayout.MarginBottom:
                float halfHeight = Size.Y * 0.5f;
                rect = new Rect2(0, halfHeight, Size.X, halfHeight);
                break;
            default:
                return;
        }

        DrawStyleBox(GetThemeStylebox("panel", "TooltipPanel"), rect);
    }

    public void SetEnabled(bool enabled, bool shouldSplit = true)
    {
        Visible = enabled;
        // An empty layout accepts its first tab in the center instead of offering edge splits.
        _shouldSplit = shouldSplit;
        if (!enabled) return;
        _drawMargin = DrawNothing;
        QueueRedraw();
    }

    public int GetHoverMargin() => _drawMargin;

    private int FindHoverMargin(Vector2 point)
    {
        // Nearest edge midpoint yields four symmetric triangular drop zones.
        Vector2 halfSize = Size * 0.5f;

        float lesser = point.DistanceSquaredTo(new Vector2(0, halfSize.Y));
        int lesserMargin = DockableLayout.MarginLeft;

        float top = point.DistanceSquaredTo(new Vector2(halfSize.X, 0));
        if (lesser > top)
        {
            lesser = top;
            lesserMargin = DockableLayout.MarginTop;
        }

        float right = point.DistanceSquaredTo(new Vector2(Size.X, halfSize.Y));
        if (lesser > right)
        {
            lesser = right;
            lesserMargin = DockableLayout.MarginRight;
        }

        float bottom = point.DistanceSquaredTo(new Vector2(halfSize.X, Size.Y));
        if (lesser > bottom)
            lesserMargin = DockableLayout.MarginBottom;

        return lesserMargin;
    }
}
