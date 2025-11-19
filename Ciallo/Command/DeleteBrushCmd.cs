using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

public class DeleteBrushCmd : CommandBase
{
    private Entity _brushE;
    private readonly BrushSetting _setting;

    public DeleteBrushCmd(Entity brushE)
    {
        _brushE = brushE;
        _setting = brushE.Get<BrushSetting>();
    }

    public override void Do()
    {
        // UI
        var bm = Document.Get<BrushManager>();
        var list = Document.Get<DocumentBrushList>();
        list.Remove(_brushE, bm);

        // Material free on its own

        // Data
        bm.Remove(_brushE);
        _brushE.Detach<ToSerializeTag>();
    }

    public override void Undo()
    {
        // Data
        _brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.Add(_brushE);

        // UI
        var list = Document.Get<DocumentBrushList>();
        list.Add(_brushE, bm);
    }
}