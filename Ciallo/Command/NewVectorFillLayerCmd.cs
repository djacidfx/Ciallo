using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillLayerCmd : CommandBase
{
    private VectorFillLayerSetting _setting = new();
    private readonly NewPolylineLayerCmd _polylineCmd;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewVectorFillLayerCmd(CommonLayerSetting commonSetting = null)
    {
        commonSetting ??= new CommonLayerSetting
        {
            Name =
            {
                Value = $"{"Vector Fill layer".Tr()} {LayerTreeNode.LayerCreationId++}",
            },
        };
        _polylineCmd = new(null, commonSetting);
    }

    protected override void BeforeFirstDo(Entity layerE)
    {
        layerE.Add(_setting);
        _polylineCmd.TargetE = layerE;
        layerE.Add(new ArrangementManager());
    }

    protected override void Do(Entity layerE)
    {
        _polylineCmd.Do();
    }

    protected override void Undo(Entity layerE)
    {
        _polylineCmd.Undo();
    }
}