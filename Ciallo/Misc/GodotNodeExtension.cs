using System.Collections.Generic;
using System.Linq;
using Godot;

using Ciallo.Misc;

public static class GodotNodeExtension
{
    public static Node GetDecedentAt(this Node node, IReadOnlyList<int> path)
    {
        if (path.Count == 0) return node;
        int idx = path[0];
        var childNode = node.GetChild(idx);
        return childNode.GetDecedentAt(path.Skip(1).ToArray());
    }
}