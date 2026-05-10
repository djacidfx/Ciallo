using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Show layers, toggle LayerTree scenes' visibility according to current working document
/// </summary>
[SceneTree, Instantiable(init: "Initialize")]
public partial class LayerPanel : VBoxContainer, IInitable
{
    public Entity Document;

    public void Init(Entity document)
    {
        Document = document;

        var opacity = Document.Get<SelectionManager>().WorkingLayer
            .Select(e => e.TryGet<CommonLayerSetting>()?.Opacity)
            .Flatten().AddTo(Document);
        LayerProperty.Opacity.BindNumber(opacity)
            .RegisterUndo(Document.Get<CommandManager>())
            .EditableIf(Document.Get<SelectionManager>().WorkingLayer, e => !e.TryHas<FolderLayerSetting>());
        Document.Add(LayerContainer);
        Document.Add(LayerContainer.RootContainer);
    }
}