using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// The visual block for a timeline track header row.
/// Structurally and visually identical to <see cref="LayerBlock"/> but does not inherit it,
/// so entities can hold both a <see cref="LayerBlock"/> (Layer panel)
/// and a <see cref="TrackHeaderBlock"/> (Timeline header) as separate Frent components.
/// </summary>
[SceneTree, Instantiable(init: "")]
public partial class TrackHeaderBlock : Container, IInitable, ILayerBlock
{
    public Entity LayerEntity { get; private set; }

    public bool IsFolder => LayerEntity.Has<FolderLayerSetting>();

    /// <summary>
    /// True when this block represents a CelFolder layer.
    /// Computed from the entity component so it is always up to date,
    /// even when the layer is marked as a CelFolder after block creation.
    /// </summary>
    public bool IsCelFolder => LayerEntity.TryGet<FolderLayerSetting>()?.IsCel ?? false;

    /// <summary>
    /// Set by <see cref="TrackTree.Create"/> before the node enters the scene tree.
    /// Avoids fragile scene-depth navigation.
    /// </summary>
    internal LayerWrapper OwningWrapper;

    public LayerWrapper Wrapper => OwningWrapper;
    public Container Node => this;

    public override void _EnterTree()
    {
        if (Wrapper != null)
            Indent.Count = Wrapper.Level - 1;
    }

    public override void _ExitTree()
    {
        Indent.Count = 0;
    }

    public void Init(Entity self)
    {
        LayerEntity = self;
        UpdateFolderIcons();
    }

    private void UpdateFolderIcons()
    {
        RegularFolderIcon.Visible = IsFolder && !IsCelFolder;
        CelFolderIcon.Visible = IsCelFolder;
    }
}
