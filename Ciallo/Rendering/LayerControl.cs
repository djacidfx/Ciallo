using Godot;
using System;

public partial class LayerControl : Control
{
    public override Variant _GetDragData(Vector2 atPosition)
    {
        return "Test";
    }
}
