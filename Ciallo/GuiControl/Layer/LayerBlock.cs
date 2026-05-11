using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable(init: "")]
public partial class LayerBlock : Container, IInitable
{
    public Entity LayerEntity;

    public bool IsFolder => DropdownArrow.Visible;

    /// <summary>
    /// True when this block represents a CelFolder layer.
    /// Computed from the entity component so it is always up to date,
    /// even when the layer is marked as a CelFolder after block creation.
    /// </summary>
    public bool IsCelFolder => LayerEntity.TryGet<FolderLayerSetting>()?.IsCelFolder ?? false;

    /// <summary>The <see cref="LayerWrapper"/> that owns this block as its Title.</summary>
    public LayerWrapper Wrapper => (LayerWrapper)GetParent();

    public override void _EnterTree()
    {
        // Every LayerBlock is the Title of its own LayerWrapper (Level N),
        // so its visual indent level is N-1.
        Indent.Count = Wrapper.Level - 1;
    }

    public override void _ExitTree()
    {
        Indent.Count = 0;
    }

    public void Init(Entity self)
    {
        LayerEntity = self;
    }
}