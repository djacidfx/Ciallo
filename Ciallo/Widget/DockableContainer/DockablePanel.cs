using Godot;

namespace Ciallo.Widget;

[Tool]
public partial class DockablePanel : TabContainer, ISerializationListener
{
    [Signal]
    public delegate void TabLayoutChangedEventHandler(int tab, DockablePanel panel);

    private string[] _trackedNames = [];

    public DockableLayoutPanel Leaf { get; private set; }

    public bool ShowTabs
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            HandleTabVisibility();
        }
    } = true;

    public bool HideSingleTab
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            HandleTabVisibility();
        }
    }

    public bool HideTabs
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
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
        BindTabSignals();
    }

    public override void _ExitTree()
    {
        UnbindTabSignals();
        base._ExitTree();
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        // Native signal connections can survive a C# tool-script reload, including old callback names.
        DisconnectLegacyTabSignals();
        if (IsInsideTree())
            BindTabSignals();
    }

    public void TrackNodes(Control[] nodes, string[] titles, DockableLayoutPanel newLeaf, bool hideTabs)
    {
        // TabContainer emits selection signals while children are rebuilt; detach the old leaf first.
        Leaf = null;

        int minSize = Mathf.Min(nodes.Length, GetChildCount());
        while (GetChildCount() > minSize)
        {
            var child = (DockableReferenceControl)GetChild(minSize);
            child.ReferenceTo = null;
            RemoveChild(child);
            child.QueueFree();
        }

        // Tabs own geometry proxies; the real controls stay direct children of DockableContainer.
        while (GetChildCount() < nodes.Length)
            AddChild(new DockableReferenceControl());

        for (int i = 0; i < nodes.Length; i++)
        {
            var refControl = (DockableReferenceControl)GetChild(i);
            refControl.ReferenceTo = nodes[i];
            refControl.Name = nodes[i].Name;
            if (GetTabTitle(i) != titles[i])
                SetTabTitle(i, titles[i]);
        }

        _trackedNames = new string[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            _trackedNames[i] = nodes[i].Name;
        HideTabs = hideTabs;
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
        Leaf = value;
        if (GetTabCount() > 0 && value != null)
        {
            // Layout indices include hidden tabs, so restore selection by name in the visible tab list.
            string currentName = value.Names[value.CurrentTab];
            int currentTab = FindVisibleTab(currentName);
            if (currentTab < 0)
                currentTab = 0;
            if (CurrentTab != currentTab)
                CurrentTab = currentTab;
        }
    }

    public Vector2 GetLayoutMinimumSize() => GetCombinedMinimumSize();

    private void OnTabSelected(long tab)
    {
        if (Leaf == null) return;

        string name = GetTabName(GetTabControl((int)tab));
        int rawIndex = Leaf.FindName(name);
        if (rawIndex < 0)
        {
            EmitSignal(SignalName.TabLayoutChanged, (int)tab, this);
            return;
        }

        Leaf.CurrentTab = rawIndex;
    }

    private void OnTabRearranged(long tab)
    {
        if (Leaf == null) return;

        var control = GetTabControl((int)tab);
        if (control == null) return;

        // Cross-panel tabs are absent from the snapshot and always emit;
        // a local tab emits only when its visible index changes.
        int previousVisibleIndex = System.Array.IndexOf(_trackedNames, GetTabName(control));
        if (previousVisibleIndex != tab)
        {
            // Capturing panel in a lambda can reload as: "Can't get method on CallableCustom 'Delegate::Invoke'".
            EmitSignal(SignalName.TabLayoutChanged, (int)tab, this);
        }
    }

    private int FindVisibleTab(string name)
    {
        for (int i = 0; i < GetTabCount(); i++)
        {
            if (GetTabName(GetTabControl(i)) == name)
                return i;
        }

        return -1;
    }

    private static string GetTabName(Control control) =>
        control is DockableReferenceControl reference ? reference.ReferenceTo.Name : control.Name;

    private void BindTabSignals()
    {
        DockableSignalConnection.EnsureConnected(
            this,
            TabContainer.SignalName.ActiveTabRearranged,
            new Callable(this, MethodName.OnTabRearranged)
        );
        DockableSignalConnection.EnsureConnected(
            this,
            TabContainer.SignalName.TabSelected,
            new Callable(this, MethodName.OnTabSelected)
        );
    }

    private void UnbindTabSignals()
    {
        DockableSignalConnection.Disconnect(
            this,
            TabContainer.SignalName.ActiveTabRearranged,
            new Callable(this, MethodName.OnTabRearranged)
        );
        DockableSignalConnection.Disconnect(
            this,
            TabContainer.SignalName.TabSelected,
            new Callable(this, MethodName.OnTabSelected)
        );
    }

    private void DisconnectLegacyTabSignals()
    {
        var legacy = new Callable(this, "OnTabChanged");
        DockableSignalConnection.Disconnect(this, TabContainer.SignalName.ActiveTabRearranged, legacy);
        DockableSignalConnection.Disconnect(this, TabContainer.SignalName.TabChanged, legacy);
    }

    private void HandleTabVisibility()
    {
        TabsVisible = ShowTabs
            && !HideTabs
            && (!HideSingleTab || GetTabCount() != 1);
    }
}
