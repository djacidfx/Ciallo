using Godot;
using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Widget;

public partial class PaintPanelContainer : Control
{
    public PaintPanel CreateAddPaintPanel(Entity document)
    {
        var panel = PaintPanel.Instantiate(document.Get<DocumentSetting>());
        AddChild(panel);
        document.Add(panel);
        return panel;
    }
    
    public void RemoveFreePaintPanel(Entity document)
    {
        document.Remove<WorldView>();
        var paintPanel = document.Get<PaintPanel>();
        document.Remove<PaintPanel>();
        paintPanel.QueueFree();
    }
}