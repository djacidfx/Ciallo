using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Tool;

public class PolylineDeleteInteractor(PolylineTransformHover hover) : InteractorBase
{
    private List<Entity> _processingEs;

    public override bool Prepare(CursorButtonData data)
    {
        var selectedEs = SelectionManager.SelectedPolylines;
        var hoveredE = hover.HoveredPolyline;
        bool isHovered = !hoveredE.IsDeletedOrNull();

        if (selectedEs.Count == 0 && !isHovered) return false;
        _processingEs = [..selectedEs];
        if (isHovered) _processingEs.Add(hoveredE);
        _processingEs = _processingEs.Distinct().ToList();

        return true;
    }

    public override void Start(CursorButtonData data)
    {
        SelectionManager.SelectedPolylines.Clear();
        var cmd = new EmptyCommand();
        foreach (var e in _processingEs)
        {
            if (e.Has<StrokeView>()) cmd.Combine(new DeleteStrokeCmd(e));
            else cmd.Combine(new DeleteFilledPolygonCmd(e));
        }
        cmd.Commit();
    }

    public override void Interacting(CursorMotionData data)
    {
    }

    public override void End(CursorButtonData data)
    {
    }

    public override void Cancel()
    {
    }
}