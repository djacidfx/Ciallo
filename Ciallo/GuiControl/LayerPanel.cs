using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Show layers, toggle LayerTree scenes' visibility according to current working document
/// </summary>
[SceneTree(root: "Root")]
public partial class LayerPanel : VBoxContainer
{
    public override void _Ready()
    {
        Root.LayerContainerPreview.QueueFree();
        Root.LayerAction.QueueFree();
    }

    public void CreateAdd(Entity document)
    {
        var layerAction = LayerAction.New(document)
            .VisibleIf(AppDocumentManager.WorkingDocument, document);
        AddChild(layerAction);
        document.Add(layerAction);

        var layerContainer = LayerContainer.Instantiate()
            .VisibleIf(AppDocumentManager.WorkingDocument, document);
        AddChild(layerContainer);
        document.Add(layerContainer);
    }

    public void RemoveFree(Entity document)
    {
        var layerContainer = document.Get<LayerContainer>();
        document.Remove<LayerContainer>();
        layerContainer.QueueFree();

        var layerAction = document.Get<LayerAction>();
        document.Remove<LayerAction>();
        layerAction.QueueFree();
    }
}