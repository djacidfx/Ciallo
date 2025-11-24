using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

public class NewBrushCmd : CommandBase
{
    private Entity _brushE;
    private readonly BrushSetting _setting;

    public NewBrushCmd(BrushSetting setting = null)
    {
        _setting = setting?.Clone() ?? new BrushSetting();
        _setting.Labels.Remove(BrushLabel.BuiltIn);
        InitEntity();
        _brushE.Add(_setting);
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_brushE);

    public override void Do()
    {
        // Data
        _brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.Add(_brushE);

        // Material
        var material = new BrushMaterial();
        material.ObserveBrushSetting(_setting);
        _brushE.Add(material);

        // UI
        // Note: Should have a dedicate custom widget to handle this.
        var list = Document.Get<DocumentBrushList>();
        list.Add(_brushE);
    }

    public override void Undo()
    {
        // UI
        var bm = Document.Get<BrushManager>();
        var list = Document.Get<DocumentBrushList>();
        list.Remove(_brushE);

        // Material
        // Note: Material is RefCounted, cannot be manually freed
        _brushE.Remove<BrushMaterial>();

        // Data
        bm.Remove(_brushE);
        _brushE.Detach<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        if (!_brushE.IsNull) return _brushE;
        _brushE = WorkingWorld.Create();

        return _brushE;
    }
}