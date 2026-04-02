using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeBrushCmd : CommandBase
{
    private StrokeBrushSetting _setting;
    public readonly Entity CopyE;

    public NewStrokeBrushCmd(StrokeBrushSetting setting = null)
    {
        _setting = setting;
    }

    public NewStrokeBrushCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity brushE)
    {
        _setting ??= CopyE.IsNull
            ? new StrokeBrushSetting()
            : CopyE.Get<StrokeBrushSetting>().Clone();
        brushE.Add(_setting);

        // View
        var material = new StrokeBrushMaterial();
        material.ObserveBrushSetting(_setting);
        brushE.Add(material);
    }

    public override void Do(Entity brushE)
    {
        // Data
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Add(brushE);

        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Add(brushE);
    }

    public override void Undo(Entity brushE)
    {
        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Remove(brushE);

        // Data
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }
}