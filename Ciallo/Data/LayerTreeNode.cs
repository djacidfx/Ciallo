using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using MessagePack;
using R3;

namespace Ciallo.Data;

[MessagePackObject(true), ToSerialize]
public class LayerTreeNode
{
    public readonly ReactiveProperty<string> Name = new("");
    public readonly ReactiveProperty<bool> IsVisible = new(true);
    
    public readonly List<Entity> Children = [];
    
    [IgnoreMember] public int ChildCount => Children.Count;
    
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
    
    public void AddDescendant(IReadOnlyList<int> parentPath, Entity child)
    {
        GetNode(parentPath).AddChild(child);
    }

    public void InsertDescendant(IReadOnlyList<int> targetPath, Entity descendant)
    {
        int idx = targetPath[^1];
        GetNode(targetPath.SkipLast(1).ToArray()).InsertChild(idx, descendant);
    }
    
    public void RemoveDescendant(IReadOnlyList<int> targetPath)
    {
        var parentNode = GetNode(targetPath.SkipLast(1).ToArray());
        parentNode.Children.RemoveAt(targetPath[^1]);
    }

    public Entity GetEntity(IReadOnlyList<int> path)
    {
        if(path.Count == 1) return Children[path[0]];
        return Children[path[0]].Get<LayerTreeNode>().GetEntity(path.Skip(1).ToArray());
    }
    
    public LayerTreeNode GetNode(IReadOnlyList<int> path)
    {
        if(path.Count == 0) return this;
        return Children[path[0]].Get<LayerTreeNode>().GetNode(path.Skip(1).ToArray());
    }
    
    public LayerTreeNode GetNodeOrNull(IReadOnlyList<int> path)
    {
        if(path.Count == 0) return this;
        int idx = path[0];
        if (idx < 0 || idx >= Children.Count) return null;
        var childNode = Children[idx].Get<LayerTreeNode>();
        return childNode.GetNodeOrNull(path.Skip(1).ToArray());
    }
    
    public List<int> GetPathTo(Entity target)
    {
        BreadthFirstSearch(this, target.Get<LayerTreeNode>(), out var path);
        return path;
    }
    
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
}