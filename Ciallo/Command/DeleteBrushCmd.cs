using Ciallo.Data;
using Ciallo.NodeControl;
using Frent;

namespace Ciallo.Command;

public class DeleteBrushCmd : CommandBase
{
    private Entity _brushE;
    private readonly BrushSetting _setting;
    private readonly CommandBase _deleteStrokeCmd;

    public DeleteBrushCmd(Entity brushE)
    {
        _brushE = brushE;
        _setting = brushE.Get<BrushSetting>();
        _deleteStrokeCmd = new EmptyCommand();

        var query = brushE.World.CreateQuery().With<StrokeSetting>().Build();
        foreach (var strokeE in query.EnumerateWithEntities())
        {
            if (strokeE.Get<StrokeSetting>().BrushE == brushE)
                _deleteStrokeCmd.Combine(new DeleteStrokeCmd(strokeE));
        }
    }

    public override void Do()
    {
        _deleteStrokeCmd.DoAllCombination();

        // UI
        var list = Document.Get<DocumentBrushList>();
        list.Remove(_brushE);

        // Material removed on its own

        // Data
        var bm = Document.Get<BrushManager>();
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
        list.Add(_brushE);

        _deleteStrokeCmd.UndoAllCombination();
    }
}