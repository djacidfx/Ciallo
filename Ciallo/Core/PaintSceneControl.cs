using Godot;
using System;

[SceneTree(root:"rt")]
public partial class PaintSceneControl : PanelContainer
{
    public override void _Process(double delta)
    {
        
    }

    public void OnResize()
    {
        var size = _Viewer.Size;
        // SubViewportContainer uses the combined size of the SubViewports as minimum size, unless stretch is enabled.
        // The stretch is enabled.
        // https://docs.godotengine.org/en/stable/classes/class_subviewportcontainer.html
    }
}
