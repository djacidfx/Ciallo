using Godot;
using System;
using Ciallo.View;

namespace Ciallo.Core;

[Tool]
public partial class WorldView : Node2D
{
    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            // var strokeView = StrokeViewManager.CreateStrokeView(
            //     [new(-200, 200), new (0, 0), new(200, -200)],
            //     [10, 10, 10]);
            // AddChild(strokeView);
            // strokeView.SetOwner(this);
        }
    }
}
