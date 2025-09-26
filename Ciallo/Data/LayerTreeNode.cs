using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Arch.Core;
using Arch.Core.Extensions;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Layer tree node component providing entity hierarchical structure like Unity's GameObject scene tree.
/// </summary>
/// <remarks> All methods this class should be named similar to and behave same as Godot's SceneTree and Node </remarks>
[DataContract, ToSerialize]
public class LayerTreeNode
{
    [DataMember] public ReactiveProperty<string> Name = new("");
    [DataMember] public ReactiveProperty<bool> IsVisible = new(true);
    
    [DataMember] public List<Entity> Children = [];
    
    public int ChildCount => Children.Count;
    public int DescendantCount => CountSubtreeNodes(this) - 1;
    public bool IsLeaf => Children.Count == 0;

    #region Modify
    
    public void AddChild(Entity child)
    {
        if (!child.Has<LayerTreeNode>()) throw new ArgumentException("Child entity must have LayerTreeNode component.");
        Children.Add(child);
    }

    public void InsertChild(int idx, Entity child)
    {
        if (!child.Has<LayerTreeNode>()) throw new ArgumentException("Child entity must have LayerTreeNode component.");
        Children.Insert(idx, child);
    }
    
    public void MoveChild(int srcIdx, int dstIdx)
    {
        var moving = Children[srcIdx];
        Children.RemoveAt(srcIdx);
        Children.Insert(dstIdx, moving);
    }
    
    public void RemoveChild(int idx)
    {
        Children.RemoveAt(idx);
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
        var removed = parentNode.Children[targetPath[^1]];
        parentNode.Children.RemoveAt(targetPath[^1]);
        return removed;
    }
    
    /// <summary>
    /// Move a descendant node to another position.
    /// </summary>
    /// <param name="srcPath">Which to move.</param>
    /// <param name="dstPath">Insertion path.</param>
    public void MoveDescendant(IReadOnlyList<int> srcPath, IReadOnlyList<int> dstPath)
    {
        // Resolve source
        var srcParentPath = srcPath.SkipLast(1).ToArray();
        var srcParent = GetDescendantNode(srcParentPath);
        int srcIdx = srcPath[^1];
        var moving = srcParent.Children[srcIdx];

        // Resolve destination parent and insertion index before mutating the tree
        var dstParentPath = dstPath.SkipLast(1).ToArray();
        int dstIdx = dstPath[^1];

        // Prevent creating a cycle by moving a node into its own subtree
        bool IsPrefix(IReadOnlyList<int> prefix, IReadOnlyList<int> full)
        {
            if (prefix.Count > full.Count) return false;
            for (int i = 0; i < prefix.Count; i++) if (prefix[i] != full[i]) return false;
            return true;
        }
        if (IsPrefix(srcPath, dstParentPath)) throw new InvalidOperationException("Cannot move a node into its own descendant.");

        var dstParent = GetDescendantNode(dstParentPath);

        if (ReferenceEquals(srcParent, dstParent))
        {
            // the same parent
            srcParent.MoveChild(srcIdx, dstIdx);
            return;
        }

        // Different parents: remove from source, then insert into destination
        srcParent.RemoveChild(srcIdx);
        dstParent.InsertChild(dstIdx, moving);
    }
    
    #endregion
    
    #region Visit

    public Entity GetDescendant([NotNull] IReadOnlyList<int> path)
    {
        if(path.Count == 0) throw new ArgumentException("Path cannot be empty.", nameof(path));
        if(path.Count == 1) return Children[path[0]];
        return Children[path[0]].Get<LayerTreeNode>().GetDescendant(path.Skip(1).ToArray());
    }
    
    public LayerTreeNode GetDescendantNode([NotNull] IReadOnlyList<int> path)
    {
        if(path.Count == 0) return this;
        return Children[path[0]].Get<LayerTreeNode>().GetDescendantNode(path.Skip(1).ToArray());
    }
    
    public LayerTreeNode GetNodeOrNull([NotNull] IReadOnlyList<int> path)
    {
        if(path.Count == 0) return this;
        int idx = path[0];
        if (idx < 0 || idx >= Children.Count) return null;
        var childNode = Children[idx].Get<LayerTreeNode>();
        return childNode.GetNodeOrNull(path.Skip(1).ToArray());
    }
    
    public List<int> SearchPathTo(Entity target)
    {
        var node = target.Get<LayerTreeNode>();
        BreadthFirstSearch(this, node, out var path);
        return path;
    }
    
    #endregion

    #region utility

    /// <summary>
    /// Breadth first search for the entity.
    /// <returns>Entity list to the target, in parent first order.</returns>
    /// </summary>
    /// <param name="node"></param>
    /// <param name="targetNode"></param>
    /// <param name="path">Indices list to the target, in parent to child order</param>
    /// <returns></returns>
    public static List<Entity> BreadthFirstSearch(LayerTreeNode node, LayerTreeNode targetNode, out List<int> path)
    {
        if(node == targetNode)
        {
            path = [];
            return [];
        }
        
        var childNodes = node.Children.Select(e => e.Get<LayerTreeNode>()).ToList();
        var index = childNodes.IndexOf(targetNode);
        if (index >= 0) // found
        {
            path = [index];
            return [node.Children[index]];
        }
        // not found, searching in children branches
        foreach (var (i, childNode) in childNodes.Index())
        {
            var ePath = BreadthFirstSearch(childNode, targetNode, out path);
            if (path == null) continue;
            path.Insert(0, i);
            ePath.Insert(0, node.Children[i]);
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

        bool Dfs(LayerTreeNode node)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                // Visit this child.
                path.Add(i);
                if (remaining == 0) return true;
                remaining--;

                // Traverse its subtree in preorder.
                var childNode = node.Children[i].Get<LayerTreeNode>();
                if (Dfs(childNode)) return true;

                // Backtrack and continue with next sibling.
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        return Dfs(this) ? path :
            throw new ArgumentOutOfRangeException(nameof(preorderIdx), "Index exceeds the number of nodes in the tree.");
    }
    
    /// <summary>
    /// Turn a path to a preorder index.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public int PathToPreorderIndex([NotNull] IReadOnlyList<int> path)
    {
        int index = 0;
        var node = this;

        for (int depth = 0; depth < path.Count; depth++)
        {
            int idx = path[depth];
            // add sizes of all sibling subtrees before idx
            for (int i = 0; i < idx; i++)
                index += CountSubtreeNodes(node.Children[i].Get<LayerTreeNode>());

            // after the first level, count the parent node itself
            if (depth > 0) index++;

            node = node.Children[idx].Get<LayerTreeNode>();
        }

        return index;
    }

    // compute total nodes in subtree (including the root of that subtree)
    public static int CountSubtreeNodes(LayerTreeNode n)
    {
        int cnt = 1;
        foreach (var e in n.Children)
            cnt += CountSubtreeNodes(e.Get<LayerTreeNode>());
        return cnt;
    }

    #endregion
}