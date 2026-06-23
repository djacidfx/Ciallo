using Ciallo.Data;
using Frent.Core;
using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable(init: "")]
public partial class LayerBlock : Container, IInitable, IDestroyable, ILayerBlock
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
    /// The <see cref="LayerWrapper"/> that owns this block as its Title, or null when the block
    /// is used standalone (e.g. a cel-child archetype row parented to an HSplitContainer).
    /// </summary>
    public virtual LayerWrapper Wrapper => GetParent() as LayerWrapper;
    public Container Node => this;

    public override void _EnterTree()
    {
        // Every LayerBlock that is the Title of its own LayerWrapper (Level N) gets indent N-1.
        // Standalone archetype rows have no owning wrapper and set their indent explicitly.
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
            : IsCel ? CelTexture : RegularFolderTexture;
    }

    private bool IsCel => LayerEntity.Tagged<CelTag>();
}
