using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class LayerTreeNode : EntityTreeNode<LayerTreeNode>
{
    public static int LayerCreationId = 0;
    /// <summary>
    /// Assume the given node is focused and going to be deleted, return the path to the next node that should have focus.
    /// e.g. Used at deletion of working layer to determine the new working layer.
    /// </summary>
    /// <param name="path">The given node path.</param>
    /// <returns>
    /// Return priority: next sibling > previous sibling > parent > empty array (no nodes after deletion)
    /// If the node is root (path is empty), return empty array.
    /// </returns>
    public ImmutableArray<int> GetNextFocusPathAfterDeletion(IReadOnlyList<int> path)
    {
        // If deleting the root, nothing to focus next.
        if (path.Count == 0)
            return [];

        // Build the parent path and resolve the parent node
        var parentPath = path
            .Take(path.Count - 1)
            .ToList();
        var parentNode = GetDescendantNode(parentPath);

        // Determine indices
        int idx = path[^1];
        int childCount = parentNode.Children.Count;

        // Next sibling
        if (idx + 1 < childCount)
        {
            parentPath.Add(idx + 1);
            return [..parentPath];
        }

        // Previous sibling
        if (idx - 1 >= 0)
        {
            parentPath.Add(idx - 1);
            return [..parentPath];
        }

        // Fallback to parent
        return [..parentPath];
    }

    // Hide ObserveReverse, Replace, Reset, Sort
    [Obsolete("LayerTreeNode disallows ObserveReplace", error: true)]
    public new Observable<CollectionReplaceEvent<Entity>> ObserveReplace() => throw new NotSupportedException();
    [Obsolete("LayerTreeNode disallows ObserveReset", error: true)]
    public new Observable<CollectionResetEvent<Entity>> ObserveReset() => throw new NotSupportedException();
    [Obsolete("LayerTreeNode disallows ObserveReverse", error: true)]
    public new Observable<(int Index, int Count)> ObserveReverse() => throw new NotSupportedException();
    [Obsolete("LayerTreeNode disallows ObserveSort", error: true)]
    public new Observable<(int Index, int Count, IComparer<Entity> Comparer)> ObserveSort() => throw new NotSupportedException();
}