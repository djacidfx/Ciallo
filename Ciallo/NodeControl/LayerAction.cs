using Ciallo.Command;
using Ciallo.Data;
using Godot;

namespace Ciallo.NodeControl;

public partial class LayerAction : Control
{
    private FileDialog _fileDialog;

    public void OnNewLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        new CommandBuilder(AppWorldManager.WorkingWorld.Value.Create())
            .NewPolylineLayer()
            .SetWorkingLayer()
            .Commit();
    }

    public void OnRemoveLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        var document = AppWorldManager.WorkingDocument.CurrentValue;
        var currentLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        if (currentLayerE.IsNull) return;

        var workingLayerPath = document.Get<SelectionManager>().WorkingLayerPath;
        var root = document.Get<LayerTreeNode>();
        var nextLayerPath = root.GetNextFocusPathAfterDeletion(workingLayerPath);

        var nextLayerE = nextLayerPath.IsEmpty ? document : root.GetDescendant(nextLayerPath);

        if (currentLayerE.Has<PolylineLayerSetting>())
            new CommandBuilder(nextLayerE)
                .SetWorkingLayer()
                .DeletePolylineLayer()
                .Commit();
        else if (currentLayerE.Has<ImageLayerSetting>())
            new CommandBuilder(nextLayerE)
                .SetWorkingLayer()
                .DeleteImageLayer()
                .Commit();
    }

    public void OnAddImage()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
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
        new CommandBuilder(AppWorldManager.WorkingWorld.Value.Create())
            .NewImageLayer(image).Commit();
    }
}