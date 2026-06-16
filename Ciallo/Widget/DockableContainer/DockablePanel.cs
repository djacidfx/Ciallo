using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool, GlobalClass]
public partial class DockablePanel : TabContainer
{
    [Signal]
    public delegate void TabLayoutChangedEventHandler(int tab);

    private DockableLayoutPanel _leaf;
    private bool _showTabs = true;
    private bool _hideSingleTab;

    public DockableLayoutPanel Leaf
    {
        get => _leaf;
        set => SetLeaf(value);
    }

    public bool ShowTabs
    {
        get => _showTabs;
        set
        {
            _showTabs = value;
            HandleTabVisibility();
        }
    }

    public bool HideSingleTab
    {
        get => _hideSingleTab;
        set
        {
            _hideSingleTab = value;
            HandleTabVisibility();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        DragToRearrangeEnabled = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        ActiveTabRearranged += OnTabChanged;
        TabSelected += OnTabSelected;
        TabChanged += OnTabChanged;
    }

    public override void _ExitTree()
    {
        ActiveTabRearranged -= OnTabChanged;
        TabSelected -= OnTabSelected;
        TabChanged -= OnTabChanged;
        base._ExitTree();
    }

    public void TrackNodes(Control[] nodes, DockableLayoutPanel newLeaf)
    {
        _leaf = null;

        int minSize = Mathf.Min(nodes.Length, GetChildCount());
        while (GetChildCount() > minSize)
        {
            var child = (DockableReferenceControl)GetChild(minSize);
            child.ReferenceTo = null;
            RemoveChild(child);
            child.QueueFree();
        }

        while (GetChildCount() < nodes.Length)
            AddChild(new DockableReferenceControl());

        for (int i = 0; i < nodes.Length; i++)
        {
            var refControl = (DockableReferenceControl)GetChild(i);
            refControl.ReferenceTo = nodes[i];
            SetTabTitle(i, nodes[i].Name);
        }

        SetLeaf(newLeaf);
        HandleTabVisibility();
    }

    public Rect2 GetChildRect()
    {
        var control = GetCurrentTabControl();
        return new Rect2(Position + control.Position, control.Size);
    }

    public void SetLeaf(DockableLayoutPanel value)
    {
        if (GetTabCount() > 0 && value != null)
            CurrentTab = Mathf.Clamp(value.CurrentTab, 0, GetTabCount() - 1);
        _leaf = value;
    }

    public Vector2 GetLayoutMinimumSize() => GetCombinedMinimumSize();

    private void OnTabSelected(long tab)
    {
        if (_leaf != null)
            _leaf.CurrentTab = (int)tab;
    }

    private void OnTabChanged(long tab)
    {
        if (_leaf == null) return;

        var control = GetTabControl((int)tab);
        if (control == null) return;

        int nameIndexInLeaf = _leaf.FindName(control.Name);
        if (nameIndexInLeaf != tab)
            EmitSignal(SignalName.TabLayoutChanged, (int)tab);
    }

    private void HandleTabVisibility()
    {
        TabsVisible = !_hideSingleTab || GetTabCount() != 1
            ? _showTabs
            : false;
    }
}
