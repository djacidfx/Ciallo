using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;

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

    public override void BeforeFirstDo(Entity targetE)
    {
        _setting ??= CopyE.IsNull
            ? new StrokeBrushSetting()
            : CopyE.Get<StrokeBrushSetting>().Clone();
        targetE.Add(_setting);

        // material
        var material = new StrokeBrushMaterial();
        material.ObserveBrushSetting(_setting);
        targetE.Add(material);

        // preview texture
        var viewport = new SubViewport()
        {
            Size = Vector2I.One * 256,
        }.QueueFreeWith(targetE);
        
        Document.Get<SubViewportHolder>().AddChild(viewport);
        var texture = new ViewportTexture()
        {
            ViewportPath = viewport.GetPath(),
        };
        targetE.Add(texture);
    }

    public override void Do(Entity targetE)
    {
        // Data
        targetE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Add(targetE);
    }

    public override void Undo(Entity brushE)
    {
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }
}