using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteBrushCmd : CommandBase
{
    public CommandBuilder SetNullBrushCmd = new();

    public override void BeforeFirstDo(Entity targetE)
    {
        if (targetE.Has<StrokeBrushSetting>())
        {
            foreach (var strokeE in targetE.World.Query<StrokeSetting>().EnumerateWithEntities())
            {
                if (strokeE.Get<StrokeSetting>().BrushE.Value != targetE) continue;
                SetNullBrushCmd.SetTarget(strokeE)
                    .SetProperty(e => e.Get<StrokeSetting>().BrushE, Entity.Null);
            }
        }
        else if (targetE.Has<VectorFillBrushSetting>())
        {
            foreach (var markerE in targetE.World.Query<VectorFillMarkerSetting>().EnumerateWithEntities())
            {
                if (markerE.Get<VectorFillMarkerSetting>().BrushE.Value != targetE) continue;
                SetNullBrushCmd.SetTarget(markerE)
                    .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, Entity.Null);
            }
        }
    }

    public override void OnDeletedAsUndo() => TargetE.Delete();

    public override void Do(Entity brushE)
    {
        SetNullBrushCmd.Do();

        // Data
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        bm.VectorFillBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity brushE)
    {
        // Data
        brushE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();

        if (brushE.Has<StrokeBrushSetting>())
            bm.StrokeBrushEs.Add(brushE);
        if (brushE.Has<VectorFillBrushSetting>())
            bm.VectorFillBrushEs.Add(brushE);

        SetNullBrushCmd.Undo();
    }
}