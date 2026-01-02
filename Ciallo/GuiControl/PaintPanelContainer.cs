using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

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
        var panel = document.Get<PaintPanel>();
        document.Remove<PaintPanel>();
        panel.QueueFree();
    }
}