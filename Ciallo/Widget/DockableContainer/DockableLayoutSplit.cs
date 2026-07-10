using System;
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

    private SplitDirection _direction;
    private float _percent = 0.5f;
    private DockableLayoutNode _first = new DockableLayoutPanel();
    private DockableLayoutNode _second = new DockableLayoutPanel();

    [Export]
    public SplitDirection Direction
    {
        get => _direction;
        set
        {
            if (value == _direction) return;
            _direction = value;
            EmitTreeChanged();
        }
    }

    [Export(PropertyHint.Range, "0,1")]
    public float Percent
    {
        get => _percent;
        set
        {
            float clampedValue = Math.Clamp(value, 0, 1);
            if (Mathf.IsEqualApprox(_percent, clampedValue)) return;
            _percent = clampedValue;
            EmitTreeChanged();
        }
    }

    [Export]
    public DockableLayoutNode First
    {
        get => _first;
        set
        {
            _first = value ?? new DockableLayoutPanel();
            _first.Parent = this;
            EmitTreeChanged();
        }
    }

    [Export]
    public DockableLayoutNode Second
    {
        get => _second;
        set
        {
            _second = value ?? new DockableLayoutPanel();
            _second.Parent = this;
            EmitTreeChanged();
        }
    }

    public DockableLayoutSplit()
    {
        ResourceName = "Split";
        _first.Parent = this;
        _second.Parent = this;
    }

    public override bool IsEmpty() => _first.IsEmpty() && _second.IsEmpty();

    public override string[] GetNames() => [.. _first.GetNames(), .. _second.GetNames()];

    public bool IsHorizontal() => _direction == SplitDirection.Horizontal;

    public bool IsVertical() => _direction == SplitDirection.Vertical;
}
