using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable(init: "")]
public partial class LayerBlock : Container, IInitable
{
    public Entity LayerEntity;

    public bool IsFolder => DropdownArrow.Visible;

    public override void _EnterTree()
    {
        var parentNode = (LayerFolderContainer)GetParent();
        int indentLevelCount = parentNode.Title == this ? parentNode.Level - 1 : parentNode.Level;
        Indent.Count = indentLevelCount;
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