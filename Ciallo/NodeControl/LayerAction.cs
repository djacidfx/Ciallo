using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.NodeControl;

public partial class LayerAction : Control
{
    private FileDialog _fileDialog;

    public void OnNewLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        var cmd = new NewPolylineLayerCmd();
        var e = cmd.InitEntity();
        cmd.Combine(new SetWorkingLayerCmd(e)).Commit();
    }

    public void OnRemoveLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        var document = AppWorldManager.WorkingDocument.CurrentValue;
        var currentLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        if (currentLayerE.IsNull) return;

        var workingLayerPath = document.Get<SelectionManager>().WorkingLayerPath;
        var nextLayerPath = document.Get<LayerTreeNode>().GetNextFocusPathAfterDeletion(workingLayerPath);

        SetWorkingLayerCmd cmd = nextLayerPath.IsEmpty ? new(Entity.Null) : new(nextLayerPath.Single());

        if (currentLayerE.Has<PolylineLayerSetting>()) cmd.Combine(new DeletePolylineLayerCmd(currentLayerE)).Commit();
        else if (currentLayerE.Has<ImageLayerSetting>()) cmd.Combine(new DeleteImageLayerCmd(currentLayerE)).Commit();
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
        new NewImageLayerCmd(image).Commit();
    }
}