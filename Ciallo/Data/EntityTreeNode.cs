using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Frent;
using Frent.Components;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Generic entity tree node component providing hierarchical structure.
/// </summary>
/// <typeparam name="T">The derived type of the tree node.</typeparam>
[DataContract]
public partial class EntityTreeNode<T> : IInitable, IDestroyable where T : EntityTreeNode<T>
{
    public Entity Self; // When this component is added to entity, assigned automatically.
    [DataMember] public Entity Parent;
    [DataMember(Name = "Children")] private readonly List<Entity> _children = [];

    private readonly Subject<TreeMutationEvent> _localMutations = new(); // local node events
    private readonly Subject<TreeMutationEvent> _mutations = new(); // include events from descendants

    public readonly Subject<CollectionAddEvent<Entity>> TreeEntered = new(); // CollectionAddEvent pass Index, Parent entity
    public readonly Subject<CollectionRemoveEvent<Entity>> TreeExited = new();
    public readonly Subject<CollectionMoveEvent<Entity>> Moved = new(); // srcIndex, dstIndex, Parent entity
    public MoveOrReparentAsExitEnter MovedAsExitEnter;

    public IReadOnlyList<Entity> Children => _children;
    public int DescendantCount => CountSubtreeNodes((T)this) - 1;
    public bool IsLeaf => _children.Count == 0;
    public bool IsRoot => Parent.IsNull;

    public void Init(Entity self)
    {
        Self = self;
        Parent = Entity.Null;
        MovedAsExitEnter = new(TreeEntered, TreeExited, Moved);
    }

    public void Destroy()
    {
        if (!Parent.IsDyingOrDead)
            Parent.Get<T>().RemoveChild(Self);
        Parent = Entity.Null;
        _children.Clear();
        Self = Entity.Null;
    }

    #region Modify

    public void AddChild(Entity child)
    {
        InsertChild(_children.Count, child);
    }

    public Entity GetChild(Index index) => _children[index];

    public void InsertChildNoSignal(int idx, Entity child)
    {
        if (!child.Has<T>()) throw new ArgumentException($"Child entity must have {typeof(T).Name} component.");
        if (idx < 0 || idx > _children.Count) throw new ArgumentOutOfRangeException(nameof(idx));
        if (child.Equals(Self)) throw new InvalidOperationException("Cannot insert self as child.");

        var childNode = child.Get<T>();
        var oldParent = childNode.Parent;
        if (!oldParent.IsNull) throw new InvalidOperationException("Node already has a parent.");

        _children.Insert(idx, child);
        childNode.Parent = Self;
    }

    public void InsertChild(int idx, Entity child)
    {
        InsertChildNoSignal(idx, child);
        var mutation = new TreeMutationEvent(TreeMutationKind.Insert, child, Entity.Null, -1, Self, idx);
        PublishLocalMutation(mutation);
    }

    /// <summary>
    /// Post-removal coordinates
    /// </summary>
    public void MoveChild(int srcIdx, int dstIdx)
    {
        if (srcIdx < 0 || srcIdx >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(srcIdx));
        if (dstIdx < 0 || dstIdx >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(dstIdx));
        if (srcIdx == dstIdx) return;

        var moving = _children[srcIdx];
        _children.RemoveAt(srcIdx);
        _children.Insert(dstIdx, moving);
        var mutation = new TreeMutationEvent(TreeMutationKind.Move, moving, Self, srcIdx, Self, dstIdx);
        PublishLocalMutation(mutation);
    }

    public Entity RemoveChildNoSignal(int i)
    {
        var removed = _children[i];
        _children.RemoveAt(i);

        if (removed.IsAlive)
            removed.Get<T>().Parent = Entity.Null;
        return removed;
    }

    public Entity RemoveChild(Index idx)
    {
        int i = idx.GetOffset(_children.Count);
        var removed = RemoveChildNoSignal(i);

        var mutation = new TreeMutationEvent(TreeMutationKind.Remove, removed, Self, i, Entity.Null, -1);
        PublishLocalMutation(mutation);

        return removed;
    }

    public int RemoveChild(Entity child)
    {
        int idx = _children.IndexOf(child);
        if (idx < 0) throw new ArgumentException("The specified entity is not a child of this node.");
        RemoveChild(idx);
        return idx;
    }

    public void RemoveAllChildren()
    {
        if (_children.Count == 0) return;

        while (_children.Count > 0)
            RemoveChild(^1);
    }

    public void AddDescendant(IReadOnlyList<int> parentPath, Entity child)
    {
        GetDescendantNode(parentPath).AddChild(child);
    }

    public void InsertDescendant(IReadOnlyList<int> targetPath, Entity descendant)
    {
        int idx = targetPath[^1];
        GetDescendantNode(targetPath.SkipLast(1).ToArray()).InsertChild(idx, descendant);
    }

    public Entity RemoveDescendant(IReadOnlyList<int> targetPath)
    {
        var parentNode = GetDescendantNode(targetPath.SkipLast(1).ToArray());
        return parentNode.RemoveChild(targetPath[^1]);
    }

    /// <summary>
    /// Move a descendant node to another position. Post-removal coordinates.
    /// </summary>
    /// <param name="srcPath">Which to move.</param>
    /// <param name="dstPath">Insertion path.</param>
    public void MoveDescendant(IReadOnlyList<int> srcPath, IReadOnlyList<int> dstPath)
    {
        // Resolve source
        var srcParentPath = srcPath.SkipLast(1).ToArray();
        var srcParent = GetDescendantNode(srcParentPath);
        int srcIdx = srcPath[^1];
        var moving = srcParent._children[srcIdx];

        // Resolve destination parent and insertion index before mutating the tree
        var dstParentPath = dstPath.SkipLast(1).ToArray();
        int dstIdx = dstPath[^1];

        // Prevent creating a cycle by moving a node into its own subtree
        bool IsPrefix(IReadOnlyList<int> prefix, IReadOnlyList<int> full)
        {
            if (prefix.Count > full.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (prefix[i] != full[i])
                    return false;
            return true;
        }

        if (IsPrefix(srcPath, dstParentPath))
            throw new InvalidOperationException("Cannot move a node into its own descendant.");

        var dstParent = GetDescendantNode(dstParentPath);
        if (ReferenceEquals(srcParent, dstParent))
        {
            // the same parent
            srcParent.MoveChild(srcIdx, dstIdx);
            return;
        }

        // Different parents: remove from source, then insert into destination
        srcParent.RemoveChildNoSignal(srcIdx);
        dstParent.InsertChildNoSignal(dstIdx, moving);
        var mutation = new TreeMutationEvent(TreeMutationKind.Move, moving, srcParent.Self, srcIdx, dstParent.Self, dstIdx);
        PublishLocalMutation(mutation);
    }

    #endregion

    #region Visit

    public T GetParentNode()
    {
        if (Parent.IsNull) return null;
        return Parent.Get<T>();
    }

    public Entity GetDescendant([NotNull] IReadOnlyList<int> path)
    {
        if (path.Count == 0) throw new ArgumentException("Path cannot be empty.", nameof(path));
        if (path.Count == 1) return _children[path[0]];
        return _children[path[0]].Get<T>().GetDescendant(path.Skip(1).ToArray());
    }

    public T GetDescendantNode([NotNull] IReadOnlyList<int> path)
    {
        if (path.Count == 0) return (T)this;
        return _children[path[0]].Get<T>().GetDescendantNode(path.Skip(1).ToArray());
    }

    public T GetNodeOrNull([NotNull] IReadOnlyList<int> path)
    {
        if (path.Count == 0) return (T)this;
        int idx = path[0];
        if (idx < 0 || idx >= _children.Count) return null;
        var childNode = _children[idx].Get<T>();
        return childNode.GetNodeOrNull(path.Skip(1).ToArray());
    }

    public ImmutableArray<int> FindPathTo(Entity target)
    {
        var node = target.Get<T>();
        BreadthFirstSearch((T)this, node, out var path);
        return [..path];
    }

    #endregion

    #region Utility

    /// <summary>
    /// Breadth first search for the entity.
    /// <returns>Entity list to the target, in parent first order.</returns>
    /// </summary>
    /// <param name="node"></param>
    /// <param name="targetNode"></param>
    /// <param name="path">Indices list to the target, in parent to child order</param>
    /// <returns></returns>
    public static List<Entity> BreadthFirstSearch(T node, T targetNode, out List<int> path)
    {
        if (node == targetNode)
        {
            path = [];
            return [];
        }

        var childNodes = node._children.Select(e => e.Get<T>()).ToList();
        var index = childNodes.IndexOf(targetNode);
        if (index >= 0) // found
        {
            path = [index];
            return [node._children[index]];
        }

        // not found, searching in children branches
        foreach (var (i, childNode) in childNodes.Index())
        {
            var ePath = BreadthFirstSearch(childNode, targetNode, out path);
            if (path == null) continue;
            path.Insert(0, i);
            ePath.Insert(0, node._children[i]);
            return ePath;
        }

        // neither found in children
        path = null;
        return null;
    }

    /// <summary>
    /// Turn a preorder index into a path.
    /// </summary>
    /// <param name="preorderIdx">Root-Left-Right ordered flatten tree index.</param>
    /// <returns>Path to the node.</returns>
    public List<int> PreorderIndexToPath(int preorderIdx)
    {
        // Preorder enumeration (parent before its children), excluding the current node (root of this subtree).
        if (preorderIdx < 0) throw new ArgumentOutOfRangeException(nameof(preorderIdx), "Index cannot be negative.");
        int remaining = preorderIdx;
        List<int> path = [];

        bool Dfs(T node)
        {
            for (int i = 0; i < node._children.Count; i++)
            {
                // Visit this child.
                path.Add(i);
                if (remaining == 0) return true;
                remaining--;

                // Traverse its subtree in preorder.
                var childNode = node._children[i].Get<T>();
                if (Dfs(childNode)) return true;

                // Backtrack and continue with next sibling.
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        return Dfs((T)this)
            ? path
            : throw new ArgumentOutOfRangeException(nameof(preorderIdx),
                "Index exceeds the number of nodes in the tree.");
    }

    /// <summary>
    /// Turn a path to a preorder index.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public int PathToPreorderIndex([NotNull] IReadOnlyList<int> path)
    {
        int index = 0;
        var node = (T)this;

        for (int depth = 0; depth < path.Count; depth++)
        {
            int idx = path[depth];
            for (int i = 0; i < idx; i++)
                index += CountSubtreeNodes(node._children[i].Get<T>());

            if (depth > 0) index++;

            node = node._children[idx].Get<T>();
        }

        return index;
    }

    // compute total nodes in subtree (including the root of that subtree)
    public static int CountSubtreeNodes(T n)
    {
        int cnt = 1;
        foreach (var e in n._children)
            cnt += CountSubtreeNodes(e.Get<T>());
        return cnt;
    }

    #endregion

    #region Event

    private void PublishLocalMutation(TreeMutationEvent mutation)
    {
        _localMutations.OnNext(mutation);
        SignalTargetFromMutation(mutation);

        BubbleMutation(mutation);
    }

    private void BubbleMutation(TreeMutationEvent mutation)
    {
        _mutations.OnNext(mutation);

        if (Parent.IsNull || !Parent.IsAlive)
            return;

        Parent.Get<T>().BubbleMutation(mutation);
    }

    private static void SignalTargetFromMutation(TreeMutationEvent mutation)
    {
        if (mutation.Target.IsNull || !mutation.Target.IsAlive || !mutation.Target.Has<T>())
            return;

        var node = mutation.Target.Get<T>();
        switch (mutation.Kind)
        {
            case TreeMutationKind.Insert:
                node.TreeEntered.OnNext(new(mutation.NewIndex, mutation.NewParent));
                break;
            case TreeMutationKind.Remove:
                node.TreeExited.OnNext(new(mutation.OldIndex, mutation.OldParent));
                break;
            case TreeMutationKind.Move:
                node.Moved.OnNext(new(mutation.OldIndex, mutation.NewIndex, mutation.NewParent));
                break;
        }
    }

    public Observable<TreeMutationEvent> ObserveMutation(bool includeDescendants = false)
    {
        return includeDescendants ? _mutations : _localMutations;
    }

    #endregion
}