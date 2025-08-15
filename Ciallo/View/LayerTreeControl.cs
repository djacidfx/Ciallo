using Godot;
using System;
using System.Linq;

public partial class LayerTreeControl : Container
{
    [Export] public PackedScene LayerControlScene;
    [Export] public ButtonGroup IsActiveLayerButtonGroup;
    
    public VBoxContainer Root;

    public override void _Ready()
    {
        Root = GetChild<VBoxContainer>(0);
        if(LayerControlScene != null && IsActiveLayerButtonGroup != null)
        {
            foreach (int i in Enumerable.Range(0, 10))
            {
                var node = CreateLayerControl();
                Root.AddChild(node);
            }
        }
    }

    public Node CreateLayerControl()
    {
        var layerRoot = LayerControlScene.Instantiate();
        var isActiveButton = layerRoot.GetNode<CheckBox>("IsActive");
        isActiveButton.ButtonGroup = IsActiveLayerButtonGroup;
        if (IsActiveLayerButtonGroup.GetPressedButton() == null)
        {
            isActiveButton.SetPressed(true);
        }
        return layerRoot;
    }
}
