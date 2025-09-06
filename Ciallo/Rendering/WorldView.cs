using Godot;
using System;

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
