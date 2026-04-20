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
        panel.Free();  // Must free instantly not queue free. Otherwise, panel could potentially get a one-frame mouse movement after closing document.
    }
}