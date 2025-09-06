using Godot;
using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Widget;

public partial class PaintPanelContainer : Control
{
    public void CreateAddPaintPanel(Entity document)
    {
        var paintPanel = PaintPanel.Instantiate(document.Get<DocumentSetting>());
        AddChild(paintPanel);
        document.Add(paintPanel);
    }
    
    public void RemoveFreePaintPanel(Entity document)
    {
        var paintPanel = document.Get<PaintPanel>();
        document.Remove<PaintPanel>();
        paintPanel.QueueFree();
    }
}