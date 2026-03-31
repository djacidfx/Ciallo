using Ciallo.Data;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteBrushCmd : CommandBase
{
    public override void BeforeFirstDo(Entity targetE) { }

    public override void Do(Entity brushE)
    {
        // UI
        if (brushE.Has<StrokeBrushSetting>())
        {
            var list = Document.Get<DocumentBrushListViewer>();
            list.Remove(brushE);
        }

        // Data
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        bm.VectorFillBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity brushE)
    {
        // Data
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();

        if (brushE.Has<StrokeBrushSetting>())
            bm.StrokeBrushEs.Add(brushE);
        if (brushE.Has<VectorFillBrushSetting>())
            bm.VectorFillBrushEs.Add(brushE);

        // UI
        if (brushE.Has<StrokeBrushSetting>())
        {
            var list = Document.Get<DocumentBrushListViewer>();
            list.Add(brushE);
        }
    }
}