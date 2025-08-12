using Godot;
using System;

namespace Ciallo.View;

public partial class LayerTreeControl : Tree
{
    public override void _Ready()
    {
        // Initialize the tree structure here
        // For example, you can add a root node and some child nodes
        var root = CreateItem();
        root.SetText(0, "Root Layer");

        var child1 = CreateItem(root);
        child1.SetText(0, "Child Layer 1");

        var child2 = CreateItem(root);
        child2.SetText(0, "Child Layer 2");
    }
}
