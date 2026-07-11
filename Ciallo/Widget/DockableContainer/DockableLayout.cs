using System.Collections.Generic;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class DockableLayout : Resource, ISerializationListener
{
    public const int MarginLeft = 0;
    public const int MarginRight = 1;
    public const int MarginTop = 2;
    public const int MarginBottom = 3;
    public const int MarginCenter = 4;

    private sealed class ChangeDispatchState
    {
        public bool Queued;
        public int SuppressionDepth;
    }

    // Godot's reload serializer restores properties before fields. Keeping transient dispatch
    // state behind a readonly, non-Variant holder prevents that restore order from resetting it.
    private readonly ChangeDispatchState _changeDispatch = new();

    // The source generator restores properties in declaration order. OnBeforeSerialize persists
    // this marker before Root/HiddenTabs hydrate, keeping reload restoration out of business events.
    private bool _restoringAfterReload { get; set; }

    [Export]
    public DockableLayoutNode Root
    {
        get;
        set
        {
            value ??= new DockableLayoutPanel();
            var rootChanged = new Callable(this, MethodName.OnRootChanged);
            if (field == value)
            {
                // Hot reload can reassign the same resource; repair transient wiring without reporting an edit.
                value.Parent = null;
                DockableSignalConnection.EnsureConnected(value, Resource.SignalName.Changed, rootChanged);
                return;
            }

            DockableLayoutNode previousRoot = field;
            field = value;
            field.Parent = null;
            DockableSignalConnection.Rebind(previousRoot, field, Resource.SignalName.Changed, rootChanged);

            QueueChanged();
        }
    } = new DockableLayoutPanel();

    [Export]
    public GodotDictionary HiddenTabs
    {
        get;
        set
        {
            value ??= [];
            if (value == field) return;
            field = value;
            QueueChanged();
        }
    } = [];

    public DockableLayout()
    {
        ResourceName = "Layout";
        SetRoot(Root, false);
    }

    public void OnBeforeSerialize()
    {
        _restoringAfterReload = true;
    }

    public void OnAfterDeserialize()
    {
        Root.Parent = null;
        DockableSignalConnection.EnsureConnected(
            Root,
            Resource.SignalName.Changed,
            new Callable(this, MethodName.OnRootChanged)
        );
        _restoringAfterReload = false;
    }

    public void SetRoot(DockableLayoutNode value, bool shouldEmitChanged = true)
    {
        if (!shouldEmitChanged)
            _changeDispatch.SuppressionDepth++;
        Root = value;
        if (!shouldEmitChanged)
            _changeDispatch.SuppressionDepth--;
    }

    public DockableLayout Clone()
    {
        // Runtime docking must not mutate the nested resources owned by the scene's default layout.
        var clone = new DockableLayout();
        clone.SetHiddenTabs(HiddenTabs.Duplicate(), false);
        clone.SetRoot(CloneNode(Root), false);
        return clone;
    }

    public string[] GetNames() => Root.GetNames();

    public void UpdateNodes(IEnumerable<string> names)
    {
        // Keep existing placement, discard stale/duplicate names, then append new panels to the first leaf.
        bool changed = false;

        var orderedNames = new List<string>(names);
        var nodeNames = new HashSet<string>(orderedNames);
        var emptyLeaves = new List<DockableLayoutPanel>();
        var leafByNodeName = new Dictionary<string, DockableLayoutPanel>();
        DockableLayoutPanel firstLeaf = null;
        EnsureNamesInNode(Root, nodeNames, emptyLeaves, leafByNodeName, ref firstLeaf);

        foreach (var leaf in emptyLeaves)
        {
            if (leaf == Root) continue;
            RemoveLeaf(leaf);
            changed = true;
        }
        firstLeaf = FindFirstLeaf(Root);

        var staleHiddenTabs = new List<Variant>();
        foreach (Variant tabName in HiddenTabs.Keys)
        {
            if (!nodeNames.Contains(tabName.AsString()))
                staleHiddenTabs.Add(tabName);
        }
        foreach (Variant tabName in staleHiddenTabs)
        {
            HiddenTabs.Remove(tabName);
            changed = true;
        }

        if (firstLeaf == null)
        {
            firstLeaf = new DockableLayoutPanel();
            SetRoot(firstLeaf);
            changed = true;
        }

        foreach (string name in orderedNames)
        {
            if (leafByNodeName.ContainsKey(name)) continue;
            firstLeaf.PushName(name);
            leafByNodeName[name] = firstLeaf;
            changed = true;
        }

        if (changed)
            OnRootChanged();
    }

    public void MoveNodeToLeaf(Node node, DockableLayoutPanel leaf, int relativePosition)
    {
        var previousLeaf = FindLeafForName(Root, node.Name);
        if (previousLeaf != null)
        {
            previousLeaf.RemoveNode(node);
            if (previousLeaf.IsEmpty())
                RemoveLeaf(previousLeaf);
        }

        leaf.InsertNode(relativePosition, node);
        OnRootChanged();
    }

    public DockableLayoutPanel GetLeafForNode(Node node) => FindLeafForName(Root, node.Name);

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

        if (Root == leaf)
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
        if (FindLeafForName(Root, node.Name) != null) return;
        FindFirstLeaf(Root).PushName(node.Name);
        OnRootChanged();
    }

    public void RemoveNode(Node node)
    {
        var leaf = FindLeafForName(Root, node.Name);
        if (leaf == null) return;
        leaf.RemoveNode(node);
        if (leaf.IsEmpty())
            RemoveLeaf(leaf);
        OnRootChanged();
    }

    public void RenameNode(string previousName, string newName)
    {
        var leaf = FindLeafForName(Root, previousName);
        if (leaf == null)
            throw new System.InvalidOperationException($"Layout node '{previousName}' was not found");
        bool wasHidden = IsTabHidden(previousName);
        leaf.RenameNode(previousName, newName);
        if (wasHidden)
        {
            HiddenTabs.Remove(previousName);
            HiddenTabs[newName] = true;
        }
        OnRootChanged();
    }

    public void SetTabHidden(string name, bool hidden)
    {
        if (FindLeafForName(Root, name) == null)
            throw new System.InvalidOperationException($"Layout node '{name}' was not found");
        if (IsTabHidden(name) == hidden) return;

        if (hidden)
            HiddenTabs[name] = true;
        else
            HiddenTabs.Remove(name);
        OnRootChanged();
    }

    public bool IsTabHidden(string name) => HiddenTabs.TryGetValue(name, out Variant value) && value.AsBool();

    public void SetNodeHidden(Node node, bool hidden) => SetTabHidden(node.Name, hidden);

    public bool IsNodeHidden(Node node) => IsTabHidden(node.Name);

    private void OnRootChanged() => QueueChanged();

    private void SetHiddenTabs(GodotDictionary value, bool shouldEmitChanged)
    {
        if (!shouldEmitChanged)
            _changeDispatch.SuppressionDepth++;
        HiddenTabs = value;
        if (!shouldEmitChanged)
            _changeDispatch.SuppressionDepth--;
    }

    private void QueueChanged()
    {
        // Immediate EmitChanged() here can keep the editor redraw spinner running forever.
        if (_restoringAfterReload || _changeDispatch.SuppressionDepth > 0 || _changeDispatch.Queued) return;
        _changeDispatch.Queued = true;
        CallDeferred(MethodName.FlushChangedSignal);
    }

    private void FlushChangedSignal()
    {
        // A deferred callable queued before a C# hard reload can outlive its managed wrapper.
        if (!_changeDispatch.Queued) return;
        _changeDispatch.Queued = false;
        EmitChanged();
    }

    private void EnsureNamesInNode(
        DockableLayoutNode node,
        HashSet<string> names,
        List<DockableLayoutPanel> emptyLeaves,
        Dictionary<string, DockableLayoutPanel> leafByNodeName,
        ref DockableLayoutPanel firstLeaf)
    {
        switch (node)
        {
            case DockableLayoutPanel panel:
                panel.UpdateNodes(names, leafByNodeName);
                if (panel.IsEmpty())
                    emptyLeaves.Add(panel);
                firstLeaf ??= panel;
                break;
            case DockableLayoutSplit split:
                EnsureNamesInNode(split.First, names, emptyLeaves, leafByNodeName, ref firstLeaf);
                EnsureNamesInNode(split.Second, names, emptyLeaves, leafByNodeName, ref firstLeaf);
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
                Names = [.. panel.Names],
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

    private static DockableLayoutPanel FindLeafForName(DockableLayoutNode node, string name)
    {
        return node switch
        {
            DockableLayoutPanel panel => panel.FindName(name) >= 0 ? panel : null,
            DockableLayoutSplit split => FindLeafForName(split.First, name) ?? FindLeafForName(split.Second, name),
            _ => throw new System.InvalidOperationException($"Invalid Resource, should be branch or leaf, found {node}"),
        };
    }

    private void RemoveLeaf(DockableLayoutPanel leaf)
    {
        if (!leaf.IsEmpty())
            throw new System.InvalidOperationException("Trying to remove a leaf with nodes");
        if (Root == leaf)
            return;

        var collapsedBranch = leaf.Parent;
        var keptBranch = leaf == collapsedBranch.Second ? collapsedBranch.First : collapsedBranch.Second;
        var rootBranch = collapsedBranch.Parent;

        // A split cannot retain an empty side, so replace it with the surviving sibling.
        if (collapsedBranch == Root)
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
