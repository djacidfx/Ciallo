using Godot;
using System;
using Ciallo.Rendering;

public partial class WorldView : Node2D
{
    public override void _Ready()
    {
        foreach (var previewNode in GetChildren())
        {
            previewNode.QueueFree();
        }
    }
}
