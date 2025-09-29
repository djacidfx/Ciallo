using Godot;
using System;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Data;

public partial class LayerAction : Control
{
    public void OnNewLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        new NewPolylineLayerCmd([0]).Combine(new ChangeWorkingLayerCmd([0])).Commit();
    }

    public void OnRemoveLayer()
    {
        if (AppWorldManager.WorkingWorld.Value == null) return;
        var document = AppWorldManager.WorkingDocument.CurrentValue;
        var workingLayerPath = document.Get<SelectionManager>().WorkingLayerPath;
        if (workingLayerPath == null) return;
        var nextLayerPath = document.Get<LayerTreeManager>().GetNextFocusPathAfterDeletion(workingLayerPath);
        new ChangeWorkingLayerCmd(nextLayerPath).Combine(new DeleteLayerCmd(workingLayerPath)).Commit();
    }
}