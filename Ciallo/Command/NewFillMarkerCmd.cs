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
public class NewFillMarkerCmd : CommandBase
{
    private NewFilledPolygonCmd _newFilledPolygonCmd;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewFillMarkerCmd(Entity layerE, FilledPolygonSetting setting = null)
    {
        _newFilledPolygonCmd = new(layerE, setting);
    }

    protected override void BeforeFirstDo(Entity targetE)
    {
        _newFilledPolygonCmd.TargetE = targetE;
    }

    protected override void Do(Entity targetE)
    {
        _newFilledPolygonCmd.Do();

        var marker = new Marker2D();
        Document.Get<WorldOverlay>().AddChild(marker);
        targetE.Add(marker);
    }

    protected override void Undo(Entity targetE)
    {
        targetE.Get<Marker2D>().QueueFree();
        targetE.Remove<Marker2D>();

        _newFilledPolygonCmd.Undo();
    }
}