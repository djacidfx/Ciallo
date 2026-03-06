using Ciallo.Data;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteBrushCmd : CommandBase
{
    public override void Do(Entity brushE)
    {
        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Remove(brushE);

        // Material removed on its own

        // Data
        var bm = Document.Get<BrushManager>();
        bm.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity brushE)
    {
        // Data
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.Add(brushE);

        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Add(brushE);
    }
}