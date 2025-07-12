using Godot;
using System;

[SceneTree(root:"rt")]
public partial class PaintSceneControl : Control
{
    public override void _Ready()
    {
        
    }

    public void OnResize()
    {
        var size = this.Size;
        // SubViewportContainer uses the combined size of the SubViewports as minimum size,
        // https://docs.godotengine.org/en/stable/classes/class_subviewportcontainer.html
        _SubViewport.Size = new(Mathf.RoundToInt(size.X), Mathf.RoundToInt(size.Y));
    }
}
