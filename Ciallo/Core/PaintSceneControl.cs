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
        _SubViewport.Size = new(Mathf.RoundToInt(Size.X), Mathf.RoundToInt(Size.Y));
    }
}
