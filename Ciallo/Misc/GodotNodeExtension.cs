using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Frent;
using Godot;
using ObservableCollections;

// ReSharper disable once CheckNamespace
namespace Ciallo;

public static class GodotNodeExtension
{
    public static Node GetDecedentAt(this Node node, IReadOnlyList<int> path)
    {
        if (path.Count == 0) return node;
        int idx = path[0];
        var childNode = node.GetChild(idx);
        return childNode.GetDecedentAt([..path.Skip(1)]);
    }

    /// <summary>
    /// Insert new node at path. Should be identical to LayerTreeNode.InsertDescendant
    /// </summary>
    public static void InsertNodeAt(this Node node, Node newNode, IReadOnlyList<int> path)
    {
        // resolve the parent to insert into (all but last index)
        var parent = node.GetDecedentAt([..path.SkipLast(1)]);
        // get insertion index
        int idx = path[^1];
        // add then move to desired position
        parent.AddChild(newNode);
        parent.MoveChild(newNode, idx);
    }

    public static void InsertNodeAt(this Node node, Node newNode, int index)
    {
        InsertNodeAt(node, newNode, [index]);
    }

    public static Node RemoveNodeAt(this Node node, IReadOnlyList<int> path)
    {
        var target = node.GetDecedentAt(path);
        target.GetParent().RemoveChild(target);
        return target;
    }

    public static void MoveNode(this Node root, IReadOnlyList<int> srcPath, IReadOnlyList<int> dstPath)
    {
        MoveNode(root, root.GetDecedentAt(srcPath), dstPath);
    }

    public static void MoveNode(this Node root, Node src, IReadOnlyList<int> dstPath)
    {
        var srcParent = src.GetParent();

        // Resolve destination parent and target index
        var dstParentPath = dstPath.SkipLast(1).ToImmutableArray();
        var dstParent = root.GetDecedentAt(dstParentPath);
        int dstIdx = dstPath[^1];

        // Prevent moving a node into its own descendant
        bool srcIsAncestorOfDst = false;
        {
            var current = dstParent;
            while (current != null)
            {
                if (ReferenceEquals(current, src))
                {
                    srcIsAncestorOfDst = true;
                    break;
                }
                current = current.GetParent();
            }
        }
        if (srcIsAncestorOfDst) throw new InvalidOperationException("Cannot move a node into its own descendant.");

        if (ReferenceEquals(srcParent, dstParent))
        {
            // Reorder within the same parent
            srcParent.MoveChild(src, dstIdx);
        }
        else
        {
            // Move across parents
            srcParent.RemoveChild(src);
            dstParent.AddChild(src);
            dstParent.MoveChild(src, dstIdx);
        }
    }

    public static ImmutableArray<int> GetIndexPathTo(this Node node, Node root)
    {
        var path = new List<int>();
        var current = node;
        while (current != null && current != root)
        {
            var parent = current.GetParent();
            if (parent == null) break; // reached the top without finding root
            int idx = current.GetIndex();
            path.Add(idx);
            current = parent;
        }

        if (current != root)
            throw new InvalidOperationException("The specified root is not an ancestor of the node.");
        path.Reverse();
        return [..path];
    }

    public static void QueueFreeChildren(this Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    public static List<Node> GetAllDescendants(this Node node)
    {
        var descendants = new List<Node>();
        foreach (Node child in node.GetChildren())
        {
            descendants.Add(child);
            descendants.AddRange(child.GetAllDescendants());
        }
        return descendants;
    }

    public static TNode AddToChildOf<TNode>(this TNode child, Node parent) where TNode : Node
    {
        parent.AddChild(child);
        return child;
    }

    // Leave tree but not be freed
    public static Node RemoveFromParent(this Node node)
    {
        node.GetParent().RemoveChild(node);
        return node;
    }

    /// <summary>
    /// Children sync with given list. Handles initial population and all incremental changes.
    /// </summary>
    public static void ObserveChildren<T>(this Node node, INotifyCollectionChangedSynchronizedViewList<T> childrenList) where T : Node
    {
        foreach (var c in childrenList)
            node.AddChild(c);

        childrenList.CollectionChanged += (_, args) =>
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < args.NewItems!.Count; i++)
                    {
                        var c = (Node)args.NewItems[i]!;
                        node.AddChild(c);
                        node.MoveChild(c, args.NewStartingIndex + i);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in args.OldItems!)
                        node.RemoveChild((Node)item!);
                    break;

                case NotifyCollectionChangedAction.Move:
                    for (int i = 0; i < args.NewItems!.Count; i++)
                        node.MoveChild((Node)args.NewItems[i]!, args.NewStartingIndex + i);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < args.OldItems!.Count; i++)
                    {
                        node.RemoveChild((Node)args.OldItems[i]!);
                        var newC = (Node)args.NewItems![i]!;
                        node.AddChild(newC);
                        node.MoveChild(newC, args.NewStartingIndex + i);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    foreach (var n in node.GetChildren())
                        node.RemoveChild(n);
                    foreach (var n in childrenList)
                        node.AddChild(n);
                    break;
            }
        };
    }
}

public static class EntityNodeExtension
{
    /// <summary>
    /// Add node as a component of the entity, and automatically free the node when the entity is deleted.
    /// </summary>
    /// <remarks>
    /// Ensure the node's lifecycle is tied to the entity's lifecycle.
    /// </remarks>
    public static void AddNode<T>(this Entity e, T node) where T : Node
    {
        e.Add(node);
        e.OnDelete += ent =>
        {
            if (!ent.Has<T>()) return;
            var n = ent.Get<T>();
            if (GodotObject.IsInstanceValid(n))
                n.QueueFree();
        };
    }

    public static T QueueFreeWith<T>(this T node, Entity e) where T : Node
    {
        e.OnDelete += _ =>
        {
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        };
        return node;
    }
}