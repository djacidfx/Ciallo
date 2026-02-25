using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillLayerCmd : CommandBase
{
    public Entity CopyE { get; }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewVectorFillLayerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting
            {
                Name = { Value = $"{"Vector fill layer".Tr()}" }
            }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var vectorFillLayerSetting = CopyE.IsNull
            ? new VectorFillLayerSetting()
            : CopyE.Get<VectorFillLayerSetting>().Clone();
        if (vectorFillLayerSetting.ReferenceLayers.Value.Any(e => e.World != targetE.World))
            vectorFillLayerSetting.ReferenceLayers.Value = [];
        targetE.Add(vectorFillLayerSetting);

        // Others
        NewShapeLayerCmd.ShapeLayerNonDataCreation(targetE);

        // Overlay extra
        var overlayHolder = targetE.Get<OverlayHolder>();
        overlayHolder.AddChild(new OverlayHolder()); // hold stroke overlay 
        overlayHolder.AddChild(new OverlayHolder()); // hold wireframe overlay
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }
    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }
}