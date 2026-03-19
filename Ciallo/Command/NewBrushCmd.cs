using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewBrushCmd : CommandBase
{
    private readonly BrushSetting _setting;

    public NewBrushCmd(BrushSetting setting = null)
    {
        _setting = setting?.Clone() ?? new BrushSetting();
        _setting.Labels.Remove(BrushLabel.BuiltIn);
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity brushE)
    {
        brushE.Add(_setting);
    }

    public override void Do(Entity brushE)
    {
        // Data
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Add(brushE);

        // Material
        var material = new BrushMaterial();
        material.ObserveBrushSetting(_setting);
        brushE.Add(material);

        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Add(brushE);
    }

    public override void Undo(Entity brushE)
    {
        // UI
        var list = Document.Get<DocumentBrushListViewer>();
        list.Remove(brushE);

        // Material
        // Note: Material is RefCounted, cannot be manually freed
        brushE.Remove<BrushMaterial>();

        // Data
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
        brushE.Remove<BrushSetting>();
    }
}