using Godot;
using System;
using Ciallo.Data;
using Ciallo.Misc;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class ToolButtonPanelContainer : Container
{
    public static PackedScene ToolButtonPanelScene = GD.Load<PackedScene>("res://NodeControl/ToolButtonPanel.tscn");
    public override void _Ready()
    {
        this.QueueFreeChildren();
        AppWorldManager.LoadedWorlds.ObserveAdd().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var root = ToolButtonPanelScene.Instantiate<ToolButtonPanel>();
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
