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
            var query = targetE.World.CreateQuery().With<StrokeSetting>().Tagged<ToSerializeTag>().Build();
            foreach (var strokeE in query.EnumerateWithEntities())
            {
                if (strokeE.Get<StrokeSetting>().BrushE.Value != targetE) continue;
                SetNullBrushCmd.SetTarget(strokeE)
                    .SetProperty(e => e.Get<StrokeSetting>().BrushE, Entity.Null);
            }
        }
        else if (targetE.Has<VectorFillBrushSetting>())
        {
            var markerQuery = targetE.World.CreateQuery().With<VectorFillMarkerSetting>().Tagged<ToSerializeTag>().Build();
            foreach (var markerE in markerQuery.EnumerateWithEntities())
            {
                if (markerE.Get<VectorFillMarkerSetting>().BrushE.Value != targetE) continue;
                SetNullBrushCmd.SetTarget(markerE)
                    .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, Entity.Null);
            }

            var polygonQuery = targetE.World.CreateQuery().With<FilledPolygonSetting>().Tagged<ToSerializeTag>().Build();
            foreach (var polygonE in polygonQuery.EnumerateWithEntities())
            {
                if (polygonE.Get<FilledPolygonSetting>().BrushE.Value != targetE) continue;
                SetNullBrushCmd.SetTarget(polygonE)
                    .SetProperty(e => e.Get<FilledPolygonSetting>().BrushE, Entity.Null);
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