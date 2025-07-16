using Godot;
using System;

namespace Ciallo.Core;

public partial class TestNode : Node
{
    public override void _Ready()
    {
        Input.UseAccumulatedInput = false;
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _Input(InputEvent e)
    {
        
    }
}