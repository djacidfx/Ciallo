using Godot;
using System;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Data;

public partial class LayerAction : Control
{
    public void OnNewLayer()
    {
        if (WorldManager.WorkingWorld.Value == null) return;
        new NewStrokeLayerCmd([0]).Combine(new ChangeWorkingLayerCmd([0])).Commit();
    }

    public void OnRemoveLayer()
    {
        if (WorldManager.WorkingWorld.Value == null) return;
        var document = WorldManager.WorkingDocument;
        var workingLayerPath = document.Get<SelectionManager>().WorkingLayerPath;
        if (workingLayerPath == null) return;
        var nextLayerPath = document.Get<LayerTreeManager>().GetNextFocusPathAfterDeletion(workingLayerPath);
        new ChangeWorkingLayerCmd(nextLayerPath).Combine(new DeleteLayerCmd(workingLayerPath)).Commit();
    }
}