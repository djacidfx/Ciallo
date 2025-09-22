using Godot;
using System;
using System.Collections.Specialized;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot.Collections;
using ObservableCollections;
using R3;

public partial class ToolPropertyPanel : PanelContainer
{
    public VBoxContainer HubBox;
    
    public static readonly PackedScene ToolPropertyHubScene = GD.Load<PackedScene>("res://NodeControl/ToolPropertyHub.tscn");

    partial class DocumentToolPropertyContainer : MarginContainer;
    
    public override void _Ready()
    {
        HubBox = GetNode<VBoxContainer>("%HubBox");
        HubBox.QueueFreeChildren();
        
        AppWorldManager.LoadedWorlds.ObserveChanged().Subscribe(et =>
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var holder = new DocumentToolPropertyContainer();
                    holder.VisibleIf(AppWorldManager.WorkingWorld, et.NewItem);
                    et.NewItem.Document().Add(holder);
                    HubBox.AddChild(holder);
                    
                    foreach (var tool in AppToolManager.GetAllTools<ToolButtonBase>())
                    {
                        var hub = ToolPropertyHubScene.Instantiate<Control>();
                        hub.VisibleIf(AppToolManager.ActiveTool, (ITool)tool);
            
                        var container = hub.GetNode<PropertyContainer>("%PropertyContainer");
                        container.QueueFreeChildren();
                        tool.DrawProperty(container, et.NewItem.Document());
            
                        var toolNameLabel = hub.GetNode<Label>("%ToolNameLabel");
                        toolNameLabel.Text = tool.ToolName;
                        
                        holder.AddChild(hub);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var box = et.OldItem.Document().Get<DocumentToolPropertyContainer>();
                    et.OldItem.Document().Remove<DocumentToolPropertyContainer>();
                    box.QueueFree();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }
}
