using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillBrushCmd : CommandBase
{
    public Entity CopyE { get; }

    public NewVectorFillBrushCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var setting = CopyE.IsNull
            ? new FillMarkerBrushSetting()
            : CopyE.Get<FillMarkerBrushSetting>().Clone();
        targetE.Add(setting);

        // View
        var strokeBrushMaterial = new BrushMaterial();
        strokeBrushMaterial.ObserveBrushSetting(setting.MarkerBrush);
        targetE.Add(strokeBrushMaterial);
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
        targetE.Document.Get<BrushManager>().VectorFillBrushEs.Add(targetE);
    }

    public override void Undo(Entity targetE)
    {
        targetE.Document.Get<BrushManager>().VectorFillBrushEs.Remove(targetE);
        targetE.Detach<ToSerializeTag>();
    }
}