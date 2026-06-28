using Ciallo.Data;
using Frent;
using Frent.Components;
using Frent.Core;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// The visual block for a timeline track header row.
/// Structurally and visually identical to <see cref="LayerBlock"/> but does not inherit it,
/// so entities can hold both a <see cref="LayerBlock"/> (Layer panel)
/// and a <see cref="TrackHeaderBlock"/> (Timeline header) as separate Frent components.
/// </summary>
[SceneTree, Instantiable(init: "")]
public partial class TrackHeaderBlock : Container, IInitable, IDestroyable, ILayerBlock
{
    private static readonly Texture2D RegularFolderTexture = GD.Load<Texture2D>("res://Icon/folder.svg");
    private static readonly Texture2D CelFolderTexture = GD.Load<Texture2D>("res://Icon/folder-animation.svg");
    private static readonly Texture2D CelTexture = GD.Load<Texture2D>("res://Icon/film.svg");
    private static readonly TagID CelTagId = Tag<CelTag>.ID;

    public Entity LayerEntity { get; private set; }

    public bool IsFolder => LayerEntity.Has<FolderLayerSetting>();

    /// <summary>
    /// True when this block represents a CelFolder layer.
    /// Computed from the entity component so it is always up to date,
    /// even when the layer is marked as a CelFolder after block creation.
    /// </summary>
    public bool IsCelFolder => LayerEntity.TryGet<FolderLayerSetting>()?.IsCelFolder ?? false;

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
        LayerEntity.OnTagged += OnLayerTagged;
        LayerEntity.OnDetach += OnLayerDetached;
        UpdateFolderIcons();
    }

    public void Destroy()
    {
        // No unsubscription, Frent clean this up.
        // LayerEntity.OnTagged -= OnLayerTagged;
        // LayerEntity.OnDetach -= OnLayerDetached;
    }

    private void OnLayerTagged(Entity entity, TagID tag)
    {
        if (tag == CelTagId)
            UpdateFolderIcons();
    }

    private void OnLayerDetached(Entity entity, TagID tag)
    {
        if (tag == CelTagId)
            UpdateFolderIcons();
    }

    private void UpdateFolderIcons()
    {
        FolderIcon.Visible = IsFolder;
        if (!IsFolder) return;

        FolderIcon.Texture = IsCelFolder
            ? CelFolderTexture
            : IsCel
                ? CelTexture
                : RegularFolderTexture;
    }

    private bool IsCel => LayerEntity.Tagged<CelTag>();
}
