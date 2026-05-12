using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Manages the layer UI controls for the Layer panel.
/// Shows every layer, including CelFolder children (cels).
/// </summary>
/// <remarks>
/// Design of node hierarchy:
/// - Root is an "implicit folder"
/// - LayerWrapper hierarchy mirrors LayerTreeNode component hierarchy exactly.
/// </remarks>
[SceneTree(root: "Root"), Instantiable]
public partial class LayerTree : LayerTreeBase
{
    public override void _Ready()
    {
        InitBase();
    }

    protected override LayerWrapper GetWrapper(Entity e) => e.Get<LayerWrapper>();
    protected override LayerBlock GetBlock(Entity e) => e.Get<LayerBlock>();

    public void Create(Entity layerE)
    {
        var wrapper = new LayerWrapper();
        wrapper.Title = LayerBlock.New();
        layerE.Add(wrapper.Block);
        layerE.AddNode(wrapper);

        InitBlock(layerE);
    }
}