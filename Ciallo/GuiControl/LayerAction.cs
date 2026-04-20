using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree(root: "Root"), Instantiable]
public partial class LayerAction : Control
{
    public Entity Document;

    public void Init(Entity document)
    {
        Document = document;
    }

    public override void _Ready()
    {
        Root.NewLayer.Pressed += OnNewShapeLayer;
        Root.RemoveLayer.Pressed += OnRemoveLayer;
        Root.NewImage.Pressed += OnNewImage;
        Root.ConvertToShape.Pressed += OnConvertToShape;
    }

    public void OnNewShapeLayer()
    {
        new CommandBuilder(Document.World.Create())
            .NewShapeLayer()
            .AddToLayerTree(AppDocumentManager.WorkingDocument.Value)
            .SetWorkingLayer()
            .Commit();
    }

    public void OnRemoveLayer()
    {
        var document = Document;
        var currentLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        if (currentLayerE.IsNull) return;

        var workingLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        var root = document.Get<LayerTreeNode>();
        var workingLayerPath = root.FindPathTo(workingLayerE);
        var nextLayerPath = root.GetNextFocusPathAfterDeletion(workingLayerPath);
        var nextLayerE = nextLayerPath.IsEmpty ? document : root.GetDescendant(nextLayerPath);

        new CommandBuilder(nextLayerE)
            .SetWorkingLayer()
            .SetTarget(currentLayerE)
            .RemoveFromLayerTree()
            .DeleteLayer()
            .Commit();
    }

    public void OnNewImage()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;
        Root.FileDialog.Popup();
    }

    public void OnImageFileSelected(string path)
    {
        Image image;
        try
        {
            image = Image.LoadFromFile(path);
        }
        catch
        {
            return;
        }
        if (image == null) return;
        new CommandBuilder(Document.World.Create())
            .NewImageLayer(image)
            .AddToLayerTree(AppDocumentManager.WorkingDocument.Value)
            .Commit();
    }

    public void OnConvertToShape()
    {
        // TODO: Convert working VectorFill layer to Shape layer
    }
}