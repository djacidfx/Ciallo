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

    protected override void Do(Entity brushE)
    {
        // Data
        brushE.Add(_setting);
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.Add(brushE);

        // Material
        var material = new BrushMaterial();
        material.ObserveBrushSetting(_setting);
        brushE.Add(material);

        // UI
        var list = Document.Get<DocumentBrushList>();
        list.Add(brushE);
    }

    protected override void Undo(Entity brushE)
    {
        var bm = Document.Get<BrushManager>();

        // UI
        var list = Document.Get<DocumentBrushList>();
        list.Remove(brushE);

        // Material
        // Note: Material is RefCounted, cannot be manually freed
        brushE.Remove<BrushMaterial>();

        // Data
        bm.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
        brushE.Remove<BrushSetting>();
    }
}