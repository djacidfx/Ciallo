using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Ciallo.Misc;

/// <summary>
/// Compare node index paths to a root. in Godot rendering order (tree preorder) 
/// </summary>
public class NodeIndexPathComparer : Comparer<ImmutableArray<int>>
{
    public static readonly NodeIndexPathComparer Instance = new NodeIndexPathComparer();

    public override int Compare(ImmutableArray<int> x, ImmutableArray<int> y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int length = Math.Min(x.Length, y.Length);
        for (int i = 0; i < length; i++)
        {
            int cmp = x[i].CompareTo(y[i]);
            if (cmp != 0) return cmp;
        }
        return x.Length.CompareTo(y.Length);
    }
}