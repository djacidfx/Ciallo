using Ciallo.Command;
using Ciallo.Data;
using Godot;

namespace Ciallo.GuiControl;

public partial class LayerAction : Control
{
    private FileDialog _fileDialog;

    public void OnNewLayer()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;
        new CommandBuilder(AppDocumentManager.WorkingDocument.Value.World.Create())
            .NewShapeLayer()
            .AddToLayerTree(AppDocumentManager.WorkingDocument.Value)
            .SetWorkingLayer()
            .Commit();
    }

    public void OnRemoveLayer()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;
        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var currentLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        if (currentLayerE.IsNull) return;

        var workingLayerPath = document.Get<SelectionManager>().WorkingLayerPath;
        var root = document.Get<LayerTreeNode>();
        var nextLayerPath = root.GetNextFocusPathAfterDeletion(workingLayerPath);

        var nextLayerE = nextLayerPath.IsEmpty ? document : root.GetDescendant(nextLayerPath);

        if (currentLayerE.Has<ShapeLayerSetting>())
            new CommandBuilder(nextLayerE)
                .SetWorkingLayer()
                .SetTarget(currentLayerE)
                .RemoveFromLayerTree()
                .DeleteShapeLayer()
                .Commit();
        else if (currentLayerE.Has<ImageLayerSetting>())
            new CommandBuilder(nextLayerE)
                .SetWorkingLayer()
                .SetTarget(currentLayerE)
                .RemoveFromLayerTree()
                .DeleteImageLayer()
                .Commit();
    }

    public void OnAddImage()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;
        _fileDialog = GetNode<FileDialog>("%FileDialog");
        _fileDialog.Popup();
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
        new CommandBuilder(AppDocumentManager.WorkingDocument.Value.World.Create())
            .NewImageLayer(image)
            .AddToLayerTree(AppDocumentManager.WorkingDocument.Value)
            .Commit();
    }
}