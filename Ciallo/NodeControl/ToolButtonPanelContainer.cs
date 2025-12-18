using Ciallo.Data;
using Ciallo.Misc;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class ToolButtonPanelContainer : Container
{
    public override void _Ready()
    {
        this.QueueFreeChildren();
        AppWorldManager.LoadedWorlds.ObserveAdd().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var panel = ToolButtonPanel.Instantiate(document);
            panel.VisibleIf(AppWorldManager.WorkingDocument, document);
            document.Add(panel);
            AddChild(panel);
        }).AddTo(this);

        AppWorldManager.LoadedWorlds.ObserveRemove().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var panel = document.Get<ToolButtonPanel>();
            panel.DeactivateToolButton();
            panel.QueueFree();
            document.Remove<ToolButtonPanel>();
        }).AddTo(this);
    }
}