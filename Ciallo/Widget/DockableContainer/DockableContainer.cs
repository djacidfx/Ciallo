using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget;

[Tool, GlobalClass, Icon("res://addons/dockable_container/icon.svg")]
public partial class DockableContainer : Container
{
    private readonly Container _panelContainer = new();
    private readonly Container _splitContainer = new();
    private readonly DockableDragNDropPanel _dragNDropPanel = new();
    private readonly Dictionary<Node, string> _nameByChild = new();
    private readonly Dictionary<string, Node> _childByName = new();

    private DockableLayout _layout;
    private DockablePanel _dragPanel;
    private TabBar.AlignmentMode _tabAlignment = TabBar.AlignmentMode.Left;
    private bool _tabsVisible = true;
    private bool _useHiddenTabsForMinSize;
    private bool _hideSingleTab;
    private int _rearrangeGroup;
    private int _currentPanelIndex;
    private int _currentSplitIndex;
    private bool _layoutDirty;

    [Export]
    public TabBar.AlignmentMode TabAlignment
    {
        get => _tabAlignment;
        set
        {
            _tabAlignment = value;
            foreach (var panel in GetPanels())
                panel.TabAlignment = value;
        }
    }

    [Export]
    public bool UseHiddenTabsForMinSize
    {
        get => _useHiddenTabsForMinSize;
        set
        {
            _useHiddenTabsForMinSize = value;
            foreach (var panel in GetPanels())
                panel.UseHiddenTabsForMinSize = value;
        }
    }

    [Export]
    public bool TabsVisible
    {
        get => _tabsVisible;
        set
        {
            _tabsVisible = value;
            foreach (var panel in GetPanels())
                panel.ShowTabs = value;
        }
    }

    [Export]
    public bool HideSingleTab
    {
        get => _hideSingleTab;
        set
        {
            _hideSingleTab = value;
            foreach (var panel in GetPanels())
                panel.HideSingleTab = value;
        }
    }

    [Export]
    public int RearrangeGroup
    {
        get => _rearrangeGroup;
        set
        {
            _rearrangeGroup = value;
            foreach (var panel in GetPanels())
                panel.SetTabsRearrangeGroup(Mathf.Max(0, value));
        }
    }

    [Export]
    public DockableLayout Layout
    {
        get => _layout;
        set => SetLayout(value);
    }

    [Export]
    public bool CloneLayoutOnReady { get; set; } = true;

    public DockableContainer()
    {
        // C# signal events can reload as: "delegate_handle.value is null" / "Can't get method on CallableCustom 'Delegate::Invoke'".
        Connect(Node.SignalName.ChildEnteredTree, new Callable(this, MethodName.OnContainerChildEnteredTree));
        Connect(Node.SignalName.ChildExitingTree, new Callable(this, MethodName.OnContainerChildExitingTree));
        SetLayout(new DockableLayout());
    }

    public override void _Ready()
    {
        base._Ready();
        SetProcessInput(false);

        _panelContainer.Name = "_panel_container";
        // Normal helper children require move_child(), which fails as: "Parent node is busy setting up children".
        AddChild(_panelContainer, false, Node.InternalMode.Front);

        _splitContainer.Name = "_split_container";
        _splitContainer.MouseFilter = MouseFilterEnum.Pass;
        _panelContainer.AddChild(_splitContainer, false, Node.InternalMode.Front);

        _dragNDropPanel.Name = "_drag_n_drop_panel";
        _dragNDropPanel.MouseFilter = MouseFilterEnum.Pass;
        _dragNDropPanel.Visible = false;
        AddChild(_dragNDropPanel, false, Node.InternalMode.Back);

        if (CloneLayoutOnReady && !Engine.IsEditorHint())
            SetLayout(_layout.Clone());
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationSortChildren)
        {
            Resort();
        }
        else if (what == NotificationDragBegin && CanHandleDragData(GetViewport().GuiGetDragData()))
        {
            _dragNDropPanel.SetEnabled(true, !_layout.Root.IsEmpty());
            SetProcessInput(true);
        }
        else if (what == NotificationDragEnd)
        {
            _dragNDropPanel.SetEnabled(false);
            SetProcessInput(false);
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is not InputEventMouseMotion) return;

        Vector2 localPosition = GetLocalMousePosition();
        DockablePanel panel = null;
        foreach (var candidate in GetPanels())
        {
            if (!candidate.GetRect().HasPoint(localPosition)) continue;
            panel = candidate;
            break;
        }

        _dragPanel = panel;
        if (panel == null) return;
        FitChildInRect(_dragNDropPanel, panel.GetChildRect());
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => CanHandleDragData(data);

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dictionary = data.AsGodotDictionary();
        var fromPath = dictionary["from_path"].AsNodePath();
        var fromNode = NormalizeDragSource(GetNode(fromPath));

        if (fromNode == _dragPanel && _dragPanel.GetChildCount() == 1)
            return;

        int tabIndex = dictionary.ContainsKey("tabc_element")
            ? dictionary["tabc_element"].AsInt32()
            : dictionary["tab_index"].AsInt32();
        var movedTab = ((TabContainer)fromNode).GetTabControl(tabIndex);
        if (movedTab is DockableReferenceControl referenceControl)
            movedTab = referenceControl.ReferenceTo;

        if (!IsManagedNode(movedTab))
        {
            movedTab.GetParent().RemoveChild(movedTab);
            AddChild(movedTab);
        }

        if (_dragPanel != null)
        {
            int margin = _dragNDropPanel.GetHoverMargin();
            _layout.SplitLeafWithNode(_dragPanel.Leaf, movedTab, margin);
        }

        _layoutDirty = true;
        QueueSort();
    }

    public void SetControlAsCurrentTab(Control control)
    {
        if (control.GetParentControl() != this)
            throw new System.InvalidOperationException("Trying to focus a control not managed by this container");
        if (IsControlHidden(control))
        {
            GD.PushWarning("Trying to focus a hidden control");
            return;
        }

        var leaf = _layout.GetLeafForNode(control);
        if (leaf == null) return;

        int positionInLeaf = leaf.FindChild(control);
        if (positionInLeaf < 0) return;

        foreach (var panel in GetPanels())
        {
            if (panel.Leaf != leaf) continue;
            panel.CurrentTab = Mathf.Clamp(positionInLeaf, 0, panel.GetTabCount() - 1);
            return;
        }
    }

    public void SetLayout(DockableLayout value)
    {
        value ??= new DockableLayout();
        if (value == _layout) return;

        var layoutChanged = new Callable(this, MethodName.OnLayoutChanged);
        if (_layout != null && _layout.IsConnected(Resource.SignalName.Changed, layoutChanged))
            _layout.Disconnect(Resource.SignalName.Changed, layoutChanged);

        _layout = value;
        // _layout.Changed += QueueSort later spams: "Error calling from signal 'changed' to callable: 'Resource::'".
        _layout.Connect(Resource.SignalName.Changed, layoutChanged);
        _layoutDirty = true;
        QueueSort();
    }

    public void SetControlHidden(Control child, bool isHidden) => _layout.SetNodeHidden(child, isHidden);

    public bool IsControlHidden(Control child) => _layout.IsNodeHidden(child);

    public Control[] GetTabs()
    {
        var tabs = new List<Control>();
        foreach (Node child in GetChildren())
        {
            if (IsManagedNode(child))
                tabs.Add((Control)child);
        }
        return tabs.ToArray();
    }

    public int GetTabCount()
    {
        int count = 0;
        foreach (Node child in GetChildren())
        {
            if (IsManagedNode(child))
                count++;
        }
        return count;
    }

    private bool CanHandleDragData(Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return false;

        var dictionary = data.AsGodotDictionary();
        string type = dictionary.TryGetValue("type", out Variant typeValue) ? typeValue.AsString() : "";
        string tabType = dictionary.TryGetValue("tab_type", out Variant tabTypeValue) ? tabTypeValue.AsString() : "";
        bool isValidType = type is "tab" or "tab_container_tab" or "tabc_element"
            || tabType is "tab_container_tab" or "tabc_element";
        if (!isValidType || !dictionary.TryGetValue("from_path", out Variant fromPathValue)) return false;

        var sourceNode = NormalizeDragSource(GetNodeOrNull(fromPathValue.AsNodePath()));
        if (sourceNode == null || !sourceNode.HasMethod("get_tabs_rearrange_group")) return false;
        Variant result = sourceNode.Call("get_tabs_rearrange_group");
        return result.AsInt32() == RearrangeGroup;
    }

    private bool IsManagedNode(Node node) =>
        node.GetParent() == this
        && node != _panelContainer
        && node != _dragNDropPanel
        && node is Control control
        && !control.TopLevel;

    private static Node NormalizeDragSource(Node node) =>
        node is TabBar ? node.GetParent() : node;

    private void UpdateLayoutWithChildren()
    {
        var names = new List<string>();
        _nameByChild.Clear();
        _childByName.Clear();

        foreach (Node child in GetChildren())
        {
            if (!TrackNode(child)) continue;
            names.Add(child.Name);
        }

        _layout.UpdateNodes(names);
        _layoutDirty = false;
    }

    private bool TrackNode(Node node)
    {
        if (!IsManagedNode(node)) return false;

        _nameByChild[node] = node.Name;
        _childByName[node.Name] = node;

        // Renamed has no sender and Callable.Bind() is unavailable; lambdas recreate "Delegate::Invoke" reload errors.
        var renamedHandler = new Callable(this, MethodName.OnTrackedChildRenamed);
        if (!node.IsConnected(Node.SignalName.Renamed, renamedHandler))
            node.Connect(Node.SignalName.Renamed, renamedHandler);
        return true;
    }

    private void TrackAndAddNode(Node node)
    {
        _nameByChild.TryGetValue(node, out string trackedName);
        if (!TrackNode(node)) return;

        if (!string.IsNullOrEmpty(trackedName) && trackedName != node.Name)
            _layout.RenameNode(trackedName, node.Name);
        _layoutDirty = true;
    }

    private void UntrackNode(Node node)
    {
        _nameByChild.Remove(node);
        _childByName.Remove(node.Name);
        var renamedHandler = new Callable(this, MethodName.OnTrackedChildRenamed);
        if (node.IsConnected(Node.SignalName.Renamed, renamedHandler))
            node.Disconnect(Node.SignalName.Renamed, renamedHandler);
        _layoutDirty = true;
    }

    private void Resort()
    {
        if (_layoutDirty)
            UpdateLayoutWithChildren();

        var rect = new Rect2(Vector2.Zero, Size);
        FitChildInRectIfChanged(this, _panelContainer, rect);
        FitChildInRectIfChanged(_panelContainer, _splitContainer, rect);

        _currentPanelIndex = 0;
        _currentSplitIndex = 0;

        var childrenList = new List<Control>();
        CalculatePanelAndSplitList(childrenList, _layout.Root);
        FitPanelAndSplitListToRect(childrenList, rect);

        UntrackChildrenAfter(_panelContainer, _currentPanelIndex);
        UntrackChildrenAfter(_splitContainer, _currentSplitIndex);
    }

    private Control CalculatePanelAndSplitList(List<Control> result, DockableLayoutNode layoutNode)
    {
        switch (layoutNode)
        {
            case DockableLayoutPanel layoutPanel:
                {
                    var nodes = new List<Control>();
                    foreach (string name in layoutPanel.Names)
                    {
                        if (!_childByName.TryGetValue(name, out var child)) continue;
                        var node = (Control)child;
                        if (IsControlHidden(node))
                            SetVisibleIfChanged(node, false);
                        else
                            nodes.Add(node);
                    }

                    if (nodes.Count == 0) return null;

                    var panel = GetPanel(_currentPanelIndex);
                    _currentPanelIndex++;
                    panel.TrackNodes(nodes.ToArray(), layoutPanel);
                    result.Add(panel);
                    return panel;
                }
            case DockableLayoutSplit layoutSplit:
                {
                    var secondResult = CalculatePanelAndSplitList(result, layoutSplit.Second);
                    var firstResult = CalculatePanelAndSplitList(result, layoutSplit.First);

                    if (firstResult != null && secondResult != null)
                    {
                        var split = GetSplit(_currentSplitIndex);
                        _currentSplitIndex++;
                        split.LayoutSplit = layoutSplit;
                        split.FirstMinimumSize = GetLayoutMinimumSize(firstResult);
                        split.SecondMinimumSize = GetLayoutMinimumSize(secondResult);
                        result.Add(split);
                        return split;
                    }

                    return firstResult ?? secondResult;
                }
            default:
                throw new System.InvalidOperationException($"Invalid Resource, should be branch or leaf, found {layoutNode}");
        }
    }

    private void FitPanelAndSplitListToRect(List<Control> panelAndSplitList, Rect2 rect)
    {
        if (panelAndSplitList.Count == 0) return;

        var control = panelAndSplitList[^1];
        panelAndSplitList.RemoveAt(panelAndSplitList.Count - 1);

        if (control is DockablePanel panel)
        {
            FitChildInRectIfChanged(_panelContainer, panel, rect);
        }
        else if (control is DockableSplitHandle split)
        {
            var splitRects = split.GetSplitRects(rect);
            FitChildInRectIfChanged(_splitContainer, split, splitRects.Self);
            FitPanelAndSplitListToRect(panelAndSplitList, splitRects.First);
            FitPanelAndSplitListToRect(panelAndSplitList, splitRects.Second);
        }
    }

    private DockablePanel GetPanel(int index)
    {
        if (index < _panelContainer.GetChildCount())
            return (DockablePanel)_panelContainer.GetChild(index);

        var panel = new DockablePanel
        {
            TabAlignment = _tabAlignment,
            ShowTabs = _tabsVisible,
            HideSingleTab = _hideSingleTab,
            UseHiddenTabsForMinSize = _useHiddenTabsForMinSize,
        };
        panel.SetTabsRearrangeGroup(Mathf.Max(0, RearrangeGroup));
        // Capturing panel in a lambda can reload as: "Can't get method on CallableCustom 'Delegate::Invoke'".
        panel.Connect(DockablePanel.SignalName.TabLayoutChanged, new Callable(this, MethodName.OnPanelTabLayoutChanged));
        _panelContainer.AddChild(panel);
        return panel;
    }

    private DockableSplitHandle GetSplit(int index)
    {
        if (index < _splitContainer.GetChildCount())
            return (DockableSplitHandle)_splitContainer.GetChild(index);

        var split = new DockableSplitHandle();
        _splitContainer.AddChild(split);
        return split;
    }

    private void UntrackChildrenAfter(Control node, int index)
    {
        while (node.GetChildCount() > index)
        {
            var child = node.GetChild(index);
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void OnPanelTabLayoutChanged(int tab, DockablePanel panel)
    {
        _layoutDirty = true;
        var control = panel.GetTabControl(tab);
        if (control is DockableReferenceControl referenceControl)
            control = referenceControl.ReferenceTo;

        if (!IsManagedNode(control))
        {
            control.GetParent().RemoveChild(control);
            AddChild(control);
        }

        _layout.MoveNodeToLeaf(control, panel.Leaf, tab);
        QueueSort();
    }

    private void OnLayoutChanged() => QueueSort();

    private void OnTrackedChildRenamed()
    {
        Node renamedChild = null;
        foreach (var child in _nameByChild.Keys)
        {
            if (_nameByChild[child] == child.Name) continue;
            renamedChild = child;
            break;
        }
        if (renamedChild != null)
            OnChildRenamed(renamedChild);
    }

    private void OnChildRenamed(Node child)
    {
        string oldName = _nameByChild[child];
        if (oldName == child.Name) return;

        _childByName.Remove(oldName);
        _nameByChild[child] = child.Name;
        _childByName[child.Name] = child;
        _layout.RenameNode(oldName, child.Name);
    }

    private void OnContainerChildEnteredTree(Node node)
    {
        if (node == _panelContainer || node == _dragNDropPanel) return;
        TrackAndAddNode(node);
    }

    private void OnContainerChildExitingTree(Node node)
    {
        if (node == _panelContainer || node == _dragNDropPanel) return;
        UntrackNode(node);
    }

    private IEnumerable<DockablePanel> GetPanels()
    {
        for (int i = 0; i < _panelContainer.GetChildCount(); i++)
            yield return (DockablePanel)_panelContainer.GetChild(i);
    }

    private static Vector2 GetLayoutMinimumSize(Control control) =>
        control switch
        {
            DockablePanel panel => panel.GetLayoutMinimumSize(),
            DockableSplitHandle split => split.GetLayoutMinimumSize(),
            _ => control.GetCombinedMinimumSize(),
        };

    private static void FitChildInRectIfChanged(Container parent, Control child, Rect2 rect)
    {
        if (child.Position.IsEqualApprox(rect.Position) && child.Size.IsEqualApprox(rect.Size))
            return;
        parent.FitChildInRect(child, rect);
    }

    private static void SetVisibleIfChanged(CanvasItem item, bool visible)
    {
        if (item.Visible == visible) return;
        item.Visible = visible;
    }
}
