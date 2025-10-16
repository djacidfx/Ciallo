using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class ToolPropertyPanel : PanelContainer
{
    public VBoxContainer HubBox;

    partial class DocumentToolPropertyContainer : MarginContainer;

    public static readonly PackedScene ToolPropertyHubScene = GD.Load<PackedScene>("res://NodeControl/ToolPropertyHub.tscn");

    public override void _Ready()
    {
        HubBox = GetNode<VBoxContainer>("%HubBox");
        HubBox.QueueFreeChildren();

        AppWorldManager.LoadedWorlds.ObserveAdd().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var holder = new DocumentToolPropertyContainer();
            holder.VisibleIf(AppWorldManager.WorkingDocument, document);
            document.Add(holder);
            HubBox.AddChild(holder);

            var toolManager = document.Get<ToolButtonPanel>();
            foreach (var tool in toolManager.GetAllTools<ToolButtonBase>())
            {
                var hub = ToolPropertyHubScene.Instantiate<Control>();
                hub.VisibleIf(toolManager.ActiveTool, tool);

                var container = hub.GetNode<PropertyContainer>("%PropertyContainer");
                container.QueueFreeChildren();
                tool.DrawProperty(container);

                hub.GetNode<Label>("%ToolNameLabel").Text = tool.ToolName;

                holder.AddChild(hub);
            }
        }).AddTo(this);

        AppWorldManager.LoadedWorlds.ObserveRemove().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var box = document.Get<DocumentToolPropertyContainer>();
            document.Remove<DocumentToolPropertyContainer>();
            box.QueueFree();
        }).AddTo(this);
    }
}