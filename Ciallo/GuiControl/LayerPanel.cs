using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Show layers, toggle LayerTree scenes' visibility according to current working document
/// </summary>
[SceneTree(root: "Root")]
public partial class LayerPanel : VBoxContainer
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
    }

    public void CreateAdd(Entity document)
    {
        var layerAction = LayerAction.New(document)
            .VisibleIf(AppDocumentManager.WorkingDocument, document);
        AddChild(layerAction);
        document.AddNode(layerAction);

        var layerProperty = LayerProperty.New()
            .VisibleIf(AppDocumentManager.WorkingDocument, document);
        AddChild(layerProperty);
        ReactiveProperty<float> opacity = document.Get<SelectionManager>().WorkingLayer
            .Select(e => e.TryGet<CommonLayerSetting>()?.Opacity)
            .Flatten().AddTo(document);
        layerProperty.Opacity.BindNumber(opacity)
            .RegisterUndo(document.Get<CommandManager>())
            .EditableIf(document.Get<SelectionManager>().WorkingLayer, e => !e.TryHas<FolderLayerSetting>());

        var layerContainer = LayerContainer.New()
            .VisibleIf(AppDocumentManager.WorkingDocument, document);
        AddChild(layerContainer);
        document.AddNode(layerContainer);
        document.AddNode(layerContainer.RootContainer);
    }

    public void RemoveFree(Entity document) { }
}