using Godot;

namespace Ciallo.Gui;

[GlobalClass, Tool]
public partial class FilePathEditor : HBoxContainer
{
    public readonly LineEdit PathEdit;
    public readonly TextureButton OpenExplorerButton;

    public FilePathEditor()
    {
        OpenExplorerButton = new TextureButton
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        
        PathEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        
        AddChild(PathEdit);
        AddChild(OpenExplorerButton);
        PathEdit.SetOwner(this);
        OpenExplorerButton.SetOwner(this);
    }
    
}