using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Manages the layer header UI for the Timeline panel.
/// Mirrors <see cref="LayerTree"/> but skips all descendants of CelFolders:
/// those layers are displayed as track rows inside the Timeline's TrackArea instead.
/// CelFolders themselves ARE shown (as the "track title" row), but their dropdown arrow
/// is hidden since they have no visible children in this tree.
/// </summary>
[SceneTree(root: "Root"), Instantiable]
public partial class TrackHeaderTree : LayerTreeBase
{
    public override void _Ready()
    {
        InitBase();
    }

    protected override LayerWrapper GetWrapper(Entity e) => e.Get<TrackHeaderWrapper>();
    protected override LayerBlock GetBlock(Entity e) => e.Get<TrackHeaderBlock>();

    /// <summary>
    /// Hides the dropdown arrow on CelFolders: their children are shown in timeline track rows,
    /// </summary>
    protected override bool ShouldShowDropdownArrow(Entity e) => e.Has<FolderLayerSetting>() && !e.Get<FolderLayerSetting>().IsCelFolder;

    /// <summary>
    /// Creates a <see cref="TrackHeaderWrapper"/> + <see cref="TrackHeaderBlock"/> for
    /// <paramref name="layerE"/> and wires all UI bindings via <see cref="LayerTreeBase.InitBlock"/>.
    /// Call once per entity from its layer-tree-node <c>Added</c> event handler,
    /// only when the parent is <em>not</em> a CelFolder.
    /// </summary>
    public void Create(Entity layerE)
    {
        var wrapper = new TrackHeaderWrapper();
        wrapper.Title = TrackHeaderBlock.New();
        layerE.Add((TrackHeaderBlock)wrapper.Block);
        layerE.AddNode(wrapper);

        InitBlock(layerE);
    }
}