using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Tool;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class ToolButtonPanelContainer : Container
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
        AppDocumentManager.LoadedDocuments.ObserveAdd().Select(et => et.Value).Subscribe(document =>
        {
            var panel = ToolButtonPanel.Instantiate(document);
            panel.VisibleIf(AppDocumentManager.WorkingDocument, document);
            document.Add(panel);
            AddChild(panel);
        }).AddTo(this);

        AppDocumentManager.LoadedDocuments.ObserveRemove().Select(et => et.Value).Subscribe(document =>
        {
            var panel = document.Get<ToolButtonPanel>();
            panel.DeactivateToolButton();
            panel.QueueFree();
            document.Remove<ToolButtonPanel>();
        }).AddTo(this);
    }
}