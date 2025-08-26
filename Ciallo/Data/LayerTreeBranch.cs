using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using MessagePack;

[assembly: MessagePackAssumedFormattable(typeof(Ciallo.Data.LayerTreeBranch))]

namespace Ciallo.Data;

/// <summary>
/// A list of entity. Adding this component implies entity is non-leaf node in the layer tree.
/// </summary>
/// <remarks>
/// Can be serialized by MessagePack by default since inherent from IList. 
/// </remarks>
public class LayerTreeBranch : List<Entity>
{
    /// <summary>
    /// Breadth first search for the entity.
    /// </summary>
    /// <returns>Indices list in the hierarchy, in parent first order.</returns>
    public List<int> FindPath(Entity target)
    {
        BreadthFirstRecursive(this, target, out var path);
        return path;
    }
    
    public void BreadthFirstRecursive(LayerTreeBranch branch, Entity target, out List<int> path)
    {
        var index = branch.IndexOf(target);
        if (index >= 0) // found
        {
            path = [index];
            return;
        }
        // not found, searching in children branches
        foreach (var (i, e) in branch.Index())
        {
            if (!e.Has<LayerTreeBranch>()) continue;
            var childBranch = e.Get<LayerTreeBranch>();
            BreadthFirstRecursive(childBranch, target, out path);
            if (path == null) continue;
            path.Insert(0, i);// prepend the index
            return;
        }
        // not found in children neither
        path = null;
    }

    /// <summary>
    /// Get the entity at the path.
    /// </summary>
    /// <param name="path">Parent first index list</param>
    /// <returns>Target entity</returns>
    public Entity GetEntityAt([NotNull] List<int> path)
    {
        return GetEntitiesAlong(path).Last();
    }
    
    /// <summary>
    /// Get the entities along the path.
    /// </summary>
    public List<Entity> GetEntitiesAlong([NotNull] List<int> path)
    {
        if (path.Count == 0) throw new ArgumentException("Path must not be empty", nameof(path));

        var currentBranch = this;
        List<Entity> entities = [];
        for (int i = 0; i < path.Count - 1; i++)
        {
            int idx = path[i];
            var e = currentBranch[idx];
            if (!e.Has<LayerTreeBranch>())
                throw new InvalidOperationException($"Entity at path index {i} does not have a child branch");
            currentBranch = e.Get<LayerTreeBranch>();
            entities.Add(e);
        }
        entities.Add(currentBranch[path[^1]]);
        
        return entities;
    }
}