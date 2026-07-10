using System.Collections.Generic;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class DockableLayout : Resource
{
    public const int MarginLeft = 0;
    public const int MarginRight = 1;
    public const int MarginTop = 2;
    public const int MarginBottom = 3;
    public const int MarginCenter = 4;

    private bool _changedSignalQueued;
    private DockableLayoutPanel _firstLeaf;
    private GodotDictionary _hiddenTabs = [];
    private readonly Dictionary<string, DockableLayoutPanel> _leafByNodeName = new();
    private DockableLayoutNode _root;

    [Export]
    public DockableLayoutNode Root
    {
        get => _root;
        set => SetRoot(value);
    }

    [Export]
    public GodotDictionary HiddenTabs
    {
        get => _hiddenTabs;
        set
        {
            if (value == _hiddenTabs) return;
            _hiddenTabs = value;
            EmitChanged();
        }
    }

    public DockableLayout()
    {
        ResourceName = "Layout";
        SetRoot(new DockableLayoutPanel(), false);
    }

    public void SetRoot(DockableLayoutNode value, bool shouldEmitChanged = true)
    {
        value ??= new DockableLayoutPanel();
        // _root.Changed += OnRootChanged can reload as: "Error calling from signal 'changed' to callable: 'Resource::'".
        var rootChanged = new Callable(this, MethodName.OnRootChanged);
        if (_root == value && _root.IsConnected(Resource.SignalName.Changed, rootChanged)) return;

        if (_root != null && _root.IsConnected(Resource.SignalName.Changed, rootChanged))
            _root.Disconnect(Resource.SignalName.Changed, rootChanged);

        _root = value;
        _root.Parent = null;
        _root.Connect(Resource.SignalName.Changed, rootChanged);

        if (shouldEmitChanged)
            OnRootChanged();
    }

    public DockableLayout Clone()
    {
        // Duplicate(true) copies signal connections too; runtime clones then inherit "Delegate::Invoke" errors.
        var clone = new DockableLayout();
        clone._hiddenTabs = _hiddenTabs.Duplicate();
        clone.SetRoot(CloneNode(_root), false);
        return clone;
    }

    public string[] GetNames() => _root.GetNames();

    public void UpdateNodes(IEnumerable<string> names)
    {
        _leafByNodeName.Clear();
        _firstLeaf = null;
        bool changed = false;

        var orderedNames = new List<string>(names);
        var nodeNames = new HashSet<string>(orderedNames);
        var emptyLeaves = new List<DockableLayoutPanel>();
        EnsureNamesInNode(_root, nodeNames, emptyLeaves);

        foreach (var leaf in emptyLeaves)
        {
            RemoveLeaf(leaf);
            changed = true;
        }
        _firstLeaf = FindFirstLeaf(_root);

        var staleHiddenTabs = new List<Variant>();
        foreach (Variant tabName in _hiddenTabs.Keys)
        {
            if (!nodeNames.Contains(tabName.AsString()))
                staleHiddenTabs.Add(tabName);
        }
        foreach (Variant tabName in staleHiddenTabs)
        {
            _hiddenTabs.Remove(tabName);
            changed = true;
        }

        if (_firstLeaf == null)
        {
            _firstLeaf = new DockableLayoutPanel();
            SetRoot(_firstLeaf);
            changed = true;
        }

        foreach (string name in orderedNames)
        {
            if (_leafByNodeName.ContainsKey(name)) continue;
            _firstLeaf.PushName(name);
            _leafByNodeName[name] = _firstLeaf;
            changed = true;
        }

        if (changed)
            OnRootChanged();
    }

    public void MoveNodeToLeaf(Node node, DockableLayoutPanel leaf, int relativePosition)
    {
        string nodeName = node.Name;
        if (_leafByNodeName.TryGetValue(nodeName, out var previousLeaf))
        {
            previousLeaf.RemoveNode(node);
            if (previousLeaf.IsEmpty())
                RemoveLeaf(previousLeaf);
        }

        leaf.InsertNode(relativePosition, node);
        _leafByNodeName[nodeName] = leaf;
        OnRootChanged();
    }

    public DockableLayoutPanel GetLeafForNode(Node node)
    {
        _leafByNodeName.TryGetValue(node.Name, out var leaf);
        return leaf;
    }

    public void SplitLeafWithNode(DockableLayoutPanel leaf, Node node, int margin)
    {
        var rootBranch = leaf.Parent;
        var newLeaf = new DockableLayoutPanel();
        var newBranch = new DockableLayoutSplit
        {
            Direction = margin is MarginLeft or MarginRight
                ? DockableLayoutSplit.SplitDirection.Horizontal
                : DockableLayoutSplit.SplitDirection.Vertical
        };

        if (margin is MarginLeft or MarginTop)
        {
            newBranch.First = newLeaf;
            newBranch.Second = leaf;
        }
        else
        {
            newBranch.First = leaf;
            newBranch.Second = newLeaf;
        }

        if (_root == leaf)
        {
            SetRoot(newBranch, false);
        }
        else if (rootBranch != null)
        {
            if (leaf == rootBranch.First)
                rootBranch.First = newBranch;
            else
                rootBranch.Second = newBranch;
        }

        MoveNodeToLeaf(node, newLeaf, 0);
    }

    public void AddNode(Node node)
    {
        string nodeName = node.Name;
        if (_leafByNodeName.ContainsKey(nodeName)) return;
        _firstLeaf ??= FindFirstLeaf(_root);
        _firstLeaf.PushName(nodeName);
        _leafByNodeName[nodeName] = _firstLeaf;
        OnRootChanged();
    }

    public void RemoveNode(Node node)
    {
        string nodeName = node.Name;
        if (!_leafByNodeName.TryGetValue(nodeName, out var leaf)) return;
        leaf.RemoveNode(node);
        _leafByNodeName.Remove(nodeName);
        if (leaf.IsEmpty())
            RemoveLeaf(leaf);
        OnRootChanged();
    }

    public void RenameNode(string previousName, string newName)
    {
        if (!_leafByNodeName.TryGetValue(previousName, out var leaf)) return;
        leaf.RenameNode(previousName, newName);
        _leafByNodeName.Remove(previousName);
        _leafByNodeName[newName] = leaf;
        OnRootChanged();
    }

    public void SetTabHidden(string name, bool hidden)
    {
        if (!_leafByNodeName.ContainsKey(name)) return;
        if (hidden)
            _hiddenTabs[name] = true;
        else
            _hiddenTabs.Remove(name);
        OnRootChanged();
    }

    public bool IsTabHidden(string name) => _hiddenTabs.TryGetValue(name, out Variant value) && value.AsBool();

    public void SetNodeHidden(Node node, bool hidden) => SetTabHidden(node.Name, hidden);

    public bool IsNodeHidden(Node node) => IsTabHidden(node.Name);

    private void OnRootChanged()
    {
        // Immediate EmitChanged() here can keep the editor redraw spinner running forever.
        if (_changedSignalQueued) return;
        _changedSignalQueued = true;
        CallDeferred(MethodName.FlushChangedSignal);
    }

    private void FlushChangedSignal()
    {
        _changedSignalQueued = false;
        EmitChanged();
    }

    private void EnsureNamesInNode(DockableLayoutNode node, HashSet<string> names, List<DockableLayoutPanel> emptyLeaves)
    {
        switch (node)
        {
            case DockableLayoutPanel panel:
                panel.UpdateNodes(names, _leafByNodeName);
                if (panel.IsEmpty())
                    emptyLeaves.Add(panel);
                _firstLeaf ??= panel;
                break;
            case DockableLayoutSplit split:
                EnsureNamesInNode(split.First, names, emptyLeaves);
                EnsureNamesInNode(split.Second, names, emptyLeaves);
                break;
            default:
                throw new System.InvalidOperationException($"Invalid Resource, should be branch or leaf, found {node}");
        }
    }

    private static DockableLayoutNode CloneNode(DockableLayoutNode node)
    {
        return node switch
        {
            DockableLayoutPanel panel => new DockableLayoutPanel
            {
                Names = panel.Names,
                CurrentTab = panel.CurrentTab,
            },
            DockableLayoutSplit split => new DockableLayoutSplit
            {
                Direction = split.Direction,
                Percent = split.Percent,
                First = CloneNode(split.First),
                Second = CloneNode(split.Second),
            },
            _ => throw new System.InvalidOperationException($"Invalid Resource, should be branch or leaf, found {node}"),
        };
    }

    private static DockableLayoutPanel FindFirstLeaf(DockableLayoutNode node)
    {
        return node switch
        {
            DockableLayoutPanel panel => panel,
            DockableLayoutSplit split => FindFirstLeaf(split.First) ?? FindFirstLeaf(split.Second),
            _ => throw new System.InvalidOperationException($"Invalid Resource, should be branch or leaf, found {node}"),
        };
    }

    private void RemoveLeaf(DockableLayoutPanel leaf)
    {
        if (!leaf.IsEmpty())
            throw new System.InvalidOperationException("Trying to remove a leaf with nodes");
        if (_root == leaf)
            return;

        var collapsedBranch = leaf.Parent;
        var keptBranch = leaf == collapsedBranch.Second ? collapsedBranch.First : collapsedBranch.Second;
        var rootBranch = collapsedBranch.Parent;

        if (collapsedBranch == _root)
        {
            SetRoot(keptBranch);
        }
        else if (rootBranch != null)
        {
            if (collapsedBranch == rootBranch.First)
                rootBranch.First = keptBranch;
            else
                rootBranch.Second = keptBranch;
        }
    }
}
