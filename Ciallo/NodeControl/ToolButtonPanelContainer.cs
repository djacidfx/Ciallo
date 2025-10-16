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
            var root = ToolButtonPanel.Instantiate(document);
            root.VisibleIf(AppWorldManager.WorkingDocument, document);
            document.Set(root);
            AddChild(root);
        }).AddTo(this);

        AppWorldManager.LoadedWorlds.ObserveRemove().Select(et => et.Value.Document()).Subscribe(document =>
        {
            document.Get<ToolButtonPanel>().QueueFree();
            document.Remove<ToolButtonPanel>();
        }).AddTo(this);
    }
}