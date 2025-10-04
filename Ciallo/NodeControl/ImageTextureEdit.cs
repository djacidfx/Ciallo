using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Misc;
using Godot;

namespace Ciallo.NodeControl;

public partial class ImageTextureEdit : BoxContainer
{
    public TextureRect TexturePreview;
    public Button LoadButton;
    public Button ClearButton;
    public FileDialog FileDialog;
    public Action<Image> ImageProcess;
    public ImageTexture Texture;
    
    [OnInstantiate]
    public void Initialise([NotNull] ImageTexture texture, Action<Image> imageProcess = null)
    {
        Texture = texture;
        ImageProcess = imageProcess;
    }
    
    public override void _EnterTree()
    {
        TexturePreview = GetNode<TextureRect>("%TexturePreview");
        LoadButton = GetNode<Button>("%LoadButton");
        ClearButton = GetNode<Button>("%ClearButton");
        FileDialog = GetNode<FileDialog>("%FileDialog");
    }

    public override void _Ready()
    {
        TexturePreview.Texture = Texture;
        
        LoadButton.Pressed += () => FileDialog.PopupCentered();
        ClearButton.Pressed += () =>
        {
            Texture.SetImage(Image.CreateEmpty(1, 1, false, Image.Format.L8));
        };
        FileDialog.FileSelected += path =>
        {
            var image = Image.LoadFromFile(path);
            if (image == null || image.IsEmpty())
            {
                var dialog = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<AcceptDialog>().Single(n => n.Name == "WarnUser");
                dialog.DialogText = "[Cannot Load Image]".Tr();
                dialog.Popup();
                return;
            }
            ImageProcess?.Invoke(image);
            Texture.SetImage(image);
        };
    }
}
