using Godot;
using System;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot.Collections;
using R3;

public partial class ToolPropertyPanel : PanelContainer
{
    public VBoxContainer HubBox;
    public readonly Dictionary<ToolButtonBase, Control> ToolToHubMap = new();
    
    private Control _currentVisibleHub;
    
    public static readonly PackedScene ToolPropertyHubScene = GD.Load<PackedScene>("res://NodeControl/ToolPropertyHub.tscn");

    public override void _Ready()
    {
        HubBox = GetNode<VBoxContainer>("%HubBox");
        HubBox.QueueFreeChildren();
        
        var tools = AppToolManager.GetAllTools<ToolButtonBase>();
        foreach (var tool in tools)
        {
            var hub = ToolPropertyHubScene.Instantiate<Control>();
            hub.Visible = false;
            var container = hub.GetNode<PropertyContainer>("%PropertyContainer");
            container.QueueFreeChildren();
            tool.DrawProperty(container);
            
            var toolNameLabel = hub.GetNode<Label>("%ToolNameLabel");
            toolNameLabel.Text = tool.ToolName;
            
            HubBox.AddChild(hub);
            ToolToHubMap.Add(tool, hub);
        }
        
        AppToolManager.ActiveTool.Subscribe(t =>
        {
            _currentVisibleHub?.SetVisible(false);
            if (t == null)
            {
                _currentVisibleHub = null;
                return;
            }
            
            var toolButtonBase = (ToolButtonBase)t;
            _currentVisibleHub = ToolToHubMap[toolButtonBase];
            _currentVisibleHub.Visible = true;
        }).AddTo(this);
    }
}
