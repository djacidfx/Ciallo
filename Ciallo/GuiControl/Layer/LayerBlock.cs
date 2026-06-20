using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable(init: "")]
public partial class LayerBlock : Container, IInitable, ILayerBlock
{
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
    /// is used standalone (e.g. a cel-child template row parented to an HSplitContainer).
    /// </summary>
    public virtual LayerWrapper Wrapper => GetParent() as LayerWrapper;
    public Container Node => this;

    public override void _EnterTree()
    {
        // Every LayerBlock that is the Title of its own LayerWrapper (Level N) gets indent N-1.
        // Standalone template rows have no owning wrapper and set their indent explicitly.
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
