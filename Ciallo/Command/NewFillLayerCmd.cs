using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFillLayerCmd : CommandBase
{
    private VectorFillLayerSetting _setting = new();
    private readonly NewPolylineLayerCmd _polylineCmd;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewFillLayerCmd(CommonLayerSetting commonSetting = null)
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