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
    public void Init(Entity document)
    {
        var opacity = document.Get<SelectionManager>().WorkingLayer
            .Select(e => e.TryGet<CommonLayerSetting>()?.Opacity)
            .Flatten().AddTo(document);
        LayerProperty.Opacity.BindNumber(opacity)
            .RegisterUndo(document.Get<CommandManager>())
            .EditableIf(document.Get<SelectionManager>().WorkingLayer, e => !e.TryHas<FolderLayerSetting>());
        document.Add(LayerTree);
        document.Add(LayerTree.RootContainer);
        LayerAction.Init(document);
    }
}