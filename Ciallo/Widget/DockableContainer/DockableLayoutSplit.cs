using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class DockableLayoutSplit : DockableLayoutNode
{
    public enum SplitDirection
    {
        Horizontal,
        Vertical
    }

    // Rejected serialized values keep the tree usable but remain visible to layout validation.
    private bool _firstReferenceInvalid;
    private bool _secondReferenceInvalid;
    private bool _percentInvalid;

    public bool HasInvalidChildReference => _firstReferenceInvalid || _secondReferenceInvalid;
    public bool HasInvalidPercent => _percentInvalid;

    [Export]
    public SplitDirection Direction
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            EmitTreeChanged();
        }
    }

    [Export(PropertyHint.Range, "0,1")]
    public float Percent
    {
        get;
        set
        {
            bool invalid = !float.IsFinite(value) || value < 0 || value > 1;
            // Keep layout math safe without hiding corrupt persisted input from validation.
            float clampedValue = float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0.5f;
            if (Mathf.IsEqualApprox(field, clampedValue) && _percentInvalid == invalid) return;
            _percentInvalid = invalid;
            field = clampedValue;
            EmitTreeChanged();
        }
    } = 0.5f;

    [Export]
    public DockableLayoutNode First
    {
        get;
        set
        {
            var child = value ?? new DockableLayoutPanel();
            if (WouldCreateCycle(child) || child == Second)
            {
                _firstReferenceInvalid = true;
                return;
            }

            _firstReferenceInvalid = false;
            if (field == child)
            {
                field.Parent = this;
                return;
            }

            if (field.Parent == this)
                field.Parent = null;
            field = child;
            field.Parent = this;
            EmitTreeChanged();
        }
    } = new DockableLayoutPanel();

    [Export]
    public DockableLayoutNode Second
    {
        get;
        set
        {
            var child = value ?? new DockableLayoutPanel();
            if (WouldCreateCycle(child) || child == First)
            {
                _secondReferenceInvalid = true;
                return;
            }

            _secondReferenceInvalid = false;
            if (field == child)
            {
                field.Parent = this;
                return;
            }

            if (field.Parent == this)
                field.Parent = null;
            field = child;
            field.Parent = this;
            EmitTreeChanged();
        }
    } = new DockableLayoutPanel();

    public DockableLayoutSplit()
    {
        ResourceName = "Split";
        // Property initializers bypass the setters, so establish transient back-links explicitly.
        First.Parent = this;
        Second.Parent = this;
    }

    public override bool IsEmpty() => First.IsEmpty() && Second.IsEmpty();

    public override string[] GetNames() => [.. First.GetNames(), .. Second.GetNames()];

    public bool IsHorizontal() => Direction == SplitDirection.Horizontal;

    public bool IsVertical() => Direction == SplitDirection.Vertical;

    private bool WouldCreateCycle(DockableLayoutNode child)
    {
        // Parent links may be incomplete during hydration; cycle detection must inspect descendants.
        var pending = new Stack<DockableLayoutNode>();
        var visited = new HashSet<ulong>();
        pending.Push(child);

        while (pending.Count > 0)
        {
            DockableLayoutNode current = pending.Pop();
            if (current == this)
                return true;
            if (!visited.Add(current.GetInstanceId()))
                continue;
            if (current is not DockableLayoutSplit split)
                continue;

            pending.Push(split.Second);
            pending.Push(split.First);
        }

        return false;
    }
}
