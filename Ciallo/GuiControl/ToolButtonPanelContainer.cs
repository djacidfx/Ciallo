using Ciallo.Data;
using Ciallo.Tool;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

public partial class ToolButtonPanelContainer : Container
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
        AppDocumentManager.LoadedDocuments.ObserveAdd()
            .Select(et => et.Value)
            .Subscribe(document =>
            {
                var panel = ToolButtonPanel.Instantiate();
                panel.Bind(document.Get<ToolManager>().PressedToolButton);
                panel.VisibleIf(AppDocumentManager.WorkingDocument, document);
                document.Add(panel);
                AddChild(panel);
            }).AddTo(this);

        AppDocumentManager.LoadedDocuments.ObserveRemove()
            .Select(et => et.Value)
            .Subscribe(document =>
            {
                var panel = document.Get<ToolButtonPanel>();
                panel.UnpressActiveButton();
                panel.QueueFree();
                document.Remove<ToolButtonPanel>();
            }).AddTo(this);
    }
}