using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

public static class GodotNodeExtension
{
    public static Node GetDecedentAt(this Node node, IReadOnlyList<int> path)
    {
        if (path.Count == 0) return node;
        int idx = path[0];
        var childNode = node.GetChild(idx);
        return childNode.GetDecedentAt(path.Skip(1).ToImmutableArray());
    }
    
    /// <summary>
    /// Insert new node at path. Should be identical to LayerTreeNode.InsertDescendant
    /// </summary>
    public static void InsertNodeAt(this Node node, Node newNode, IReadOnlyList<int> path)
    {
        // resolve the parent to insert into (all but last index)
        var parent = node.GetDecedentAt(path.SkipLast(1).ToImmutableArray());
        // get insertion index
        int idx = path[^1];
        // add then move to desired position
        parent.AddChild(newNode);
        parent.MoveChild(newNode, idx);
    }
    
    public static Node RemoveNodeAt(this Node node, IReadOnlyList<int> path)
    {
        var target = node.GetDecedentAt(path);
        target.GetParent().RemoveChild(target);
        return target;
    }
    
    // Gen by asking copilot refer to LayerTreeNode.MoveDescendant
    public static void MoveNode(this Node node, IReadOnlyList<int> srcPath, IReadOnlyList<int> dstPath)
    {
        // Resolve source parent and node to move
        var srcParent = node.GetDecedentAt(srcPath.SkipLast(1).ToImmutableArray());
        int srcIdx = srcPath[^1];
        var moving = srcParent.GetChild(srcIdx);

        // Resolve destination parent and target index
        var dstParentPath = dstPath.SkipLast(1).ToImmutableArray();
        var dstParent = node.GetDecedentAt(dstParentPath);
        int dstIdx = dstPath[^1];

        // Prevent moving a node into its own descendant
        bool IsPrefix(IReadOnlyList<int> prefix, IReadOnlyList<int> full)
        {
            if (prefix.Count > full.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (prefix[i] != full[i]) return false;
            return true;
        }
        if (IsPrefix(srcPath, dstParentPath))
            throw new InvalidOperationException("Cannot move a node into its own descendant.");

        if (ReferenceEquals(srcParent, dstParent))
        {
            // Reorder within the same parent
            srcParent.MoveChild(moving, dstIdx);
        }
        else
        {
            // Move across parents
            srcParent.RemoveChild(moving);
            dstParent.AddChild(moving);
            dstParent.MoveChild(moving, dstIdx);
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
}