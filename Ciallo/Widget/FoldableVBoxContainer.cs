using Godot;
using R3;

namespace Ciallo.Widget;

/// <summary>
/// Container that behaves like VBoxContainer with optional reverse-order and fold/unfold.
/// Pitfall: Extends Container directly because BoxContainer overrides _get_minimum_size entirely in
/// C++ without calling GDVIRTUAL_CALL, so a C# override would never be invoked.
/// </summary>
[GlobalClass, Tool]
public partial class FoldableVBoxContainer : Container
{
    [Export]
    public bool ReverseOrder
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            UpdateMinimumSize();
            QueueSort();
        }
    } = true;

    [Export]
    public bool IsExpanded
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            UpdateMinimumSize();
            QueueSort();
        }
    } = true;

    [Export]
    public int Separation
    {
        get;
        set
        {
            field = value;
            UpdateMinimumSize();
            QueueSort();
        }
    }

    public FoldableVBoxContainer()
    {
        ClipContents = true;
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateMinimumSize();
        QueueSort();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationSortChildren)
            DoLayout();
        else if (what == NotificationThemeChanged)
        {
            UpdateMinimumSize();
            QueueSort();
        }
    }

    /// <summary>
    /// The title Control added as an internal back-node.
    /// </summary>
    public Control Title
    {
        get
        {
            int childCount = GetChildCount();
            if (GetChildCount(true) == childCount) // no internal nodes
                return null;
            return (Control)GetChild(childCount, true);
        }
        set
        {
            var oldTitle = Title;
            if (oldTitle != null)
            {
                RemoveChild(oldTitle);
            }
            if (value == null) return;
            AddChild(value, false, InternalMode.Back);
        }
    }

    private void SetExpanded(bool value) => IsExpanded = value;

    private void DoLayout()
    {
        if (!IsVisibleInTree()) return;

        int separation = Separation;
        var title = Title;
        float containerWidth = Size.X;
        float titleH = title != null ? title.GetCombinedMinimumSize().Y : 0;
        int childCount = GetChildCount();

        if (!IsExpanded)
        {
            if (title != null)
                FitChildInRect(title, new Rect2(0, 0, containerWidth, titleH));
            // Push content children fully below the clip boundary so they are invisible.
            for (int i = 0; i < childCount; i++)
            {
                if (GetChild(i) is Control c && c.Visible && !c.IsSetAsTopLevel())
                    c.Position = new Vector2(c.Position.X, Size.Y);
            }
            return;
        }

        // ----- Vertical box layout with EXPAND support -----
        // Step 1: total minimum height and stretch info for expanding children.
        int layoutChildCount = 0;
        float totalMinH = 0;
        float totalStretch = 0;
        for (int i = 0; i < childCount; i++)
        {
            if (GetChild(i) is not Control c || !c.Visible || c.IsSetAsTopLevel()) continue;
            if (layoutChildCount > 0) totalMinH += separation;
            totalMinH += c.GetCombinedMinimumSize().Y;
            if ((c.SizeFlagsVertical & SizeFlags.Expand) != 0)
                totalStretch += c.SizeFlagsStretchRatio;
            layoutChildCount++;
        }

        float titleSep = title != null && layoutChildCount > 0 ? separation : 0;
        float availableForContent = Size.Y - titleH - titleSep;
        float extraSpace = totalStretch > 0 ? Mathf.Max(0f, availableForContent - totalMinH) : 0f;

        // Step 2: title is always at the top; ReverseOrder only affects content order.
        float contentOffset = 0;
        if (title != null)
        {
            FitChildInRect(title, new Rect2(0, 0, containerWidth, titleH));
            contentOffset = titleH + titleSep;
        }

        if (ReverseOrder)
        {
            float offset = contentOffset;
            int placed = 0;
            for (int i = childCount - 1; i >= 0; i--)
            {
                if (GetChild(i) is not Control c || !c.Visible || c.IsSetAsTopLevel()) continue;
                float height = c.GetCombinedMinimumSize().Y;
                if ((c.SizeFlagsVertical & SizeFlags.Expand) != 0 && totalStretch > 0)
                    height += extraSpace * (c.SizeFlagsStretchRatio / totalStretch);
                FitChildInRect(c, new Rect2(0, offset, containerWidth, height));
                offset += height;
                if (++placed < layoutChildCount) offset += separation;
            }
        }
        else
        {
            float offset = contentOffset;
            int placed = 0;
            for (int i = 0; i < childCount; i++)
            {
                if (GetChild(i) is not Control c || !c.Visible || c.IsSetAsTopLevel()) continue;
                float height = c.GetCombinedMinimumSize().Y;
                if ((c.SizeFlagsVertical & SizeFlags.Expand) != 0 && totalStretch > 0)
                    height += extraSpace * (c.SizeFlagsStretchRatio / totalStretch);
                FitChildInRect(c, new Rect2(0, offset, containerWidth, height));
                offset += height;
                if (++placed < layoutChildCount) offset += separation;
            }
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        int separation = Separation;
        var title = Title;

        float width = 0;
        float height = 0;
        bool first = true;

        if (title != null)
        {
            Vector2 titleMin = title.GetCombinedMinimumSize();
            height = titleMin.Y;
            width = titleMin.X;
            first = false;
        }

        // Order doesn't affect total size; ReverseOrder only changes visual positioning.
        if (IsExpanded)
        {
            int childCount = GetChildCount();
            for (int i = 0; i < childCount; i++)
            {
                if (GetChild(i) is not Control c || !c.Visible || c.IsSetAsTopLevel()) continue;
                Vector2 childMin = c.GetCombinedMinimumSize();
                if (!first) height += separation;
                first = false;
                height += childMin.Y;
                width = Mathf.Max(width, childMin.X);
            }
        }

        return new Vector2(width, height);
    }

    public FoldableVBoxContainer ObserveIsExpanded(ReactiveProperty<bool> property, CompositeDisposable subs)
    {
        property.Subscribe(v => IsExpanded = v).AddTo(subs);
        return this;
    }
}
