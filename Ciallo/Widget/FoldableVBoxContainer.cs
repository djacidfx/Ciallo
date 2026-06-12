using System.Collections.Generic;
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
        int separation = Separation;
        var title = Title;
        float containerWidth = Size.X;

        // Gather visible content children (non-internal).
        var children = new List<Control>();
        foreach (Node child in GetChildren())
        {
            if (child is Control c && c.Visible && !c.IsSetAsTopLevel())
                children.Add(c);
        }

        float titleH = title != null ? title.GetCombinedMinimumSize().Y : 0;

        if (!IsExpanded)
        {
            if (title != null)
                FitChildInRect(title, new Rect2(0, 0, containerWidth, titleH));
            // Push content children fully below the clip boundary so they are invisible.
            foreach (var c in children)
                c.Position = new Vector2(c.Position.X, Size.Y);
            return;
        }

        // ----- Vertical box layout with EXPAND support -----
        // Step 1: total minimum height and stretch info for expanding children.
        float totalMinH = 0;
        float totalStretch = 0;
        for (int i = 0; i < children.Count; i++)
        {
            if (i > 0) totalMinH += separation;
            totalMinH += children[i].GetCombinedMinimumSize().Y;
            if ((children[i].SizeFlagsVertical & SizeFlags.Expand) != 0)
                totalStretch += children[i].SizeFlagsStretchRatio;
        }

        float titleSep = (title != null && children.Count > 0) ? separation : 0;
        float availableForContent = Size.Y - titleH - titleSep;
        float extraSpace = totalStretch > 0 ? Mathf.Max(0f, availableForContent - totalMinH) : 0f;

        // Step 2: compute final height per child.
        var heights = new float[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            heights[i] = children[i].GetCombinedMinimumSize().Y;
            if ((children[i].SizeFlagsVertical & SizeFlags.Expand) != 0 && totalStretch > 0)
                heights[i] += extraSpace * (children[i].SizeFlagsStretchRatio / totalStretch);
        }

        // Step 3: title is always at the top; ReverseOrder only affects content order.
        float contentOffset = 0;
        if (title != null)
        {
            FitChildInRect(title, new Rect2(0, 0, containerWidth, titleH));
            contentOffset = titleH + titleSep;
        }

        if (ReverseOrder)
        {
            float offset = contentOffset;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                FitChildInRect(children[i], new Rect2(0, offset, containerWidth, heights[i]));
                offset += heights[i] + (i > 0 ? separation : 0);
            }
        }
        else
        {
            float offset = contentOffset;
            for (int i = 0; i < children.Count; i++)
            {
                FitChildInRect(children[i], new Rect2(0, offset, containerWidth, heights[i]));
                offset += heights[i] + (i < children.Count - 1 ? separation : 0);
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

        void AddItem(Vector2 itemMin)
        {
            if (!first) height += separation;
            else first = false;
            height += itemMin.Y;
            width = Mathf.Max(width, itemMin.X);
        }

        // Order doesn't affect total size; ReverseOrder only changes visual positioning.
        if (title != null)
            AddItem(title.GetCombinedMinimumSize());

        if (IsExpanded)
        {
            foreach (Node child in GetChildren())
            {
                if (child is Control c && c.Visible && !c.IsSetAsTopLevel())
                    AddItem(c.GetCombinedMinimumSize());
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