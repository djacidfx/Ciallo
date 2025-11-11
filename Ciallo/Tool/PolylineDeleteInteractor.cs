using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Tool;

public class PolylineDeleteInteractor(PolylineTransformHover hover) : InteractorBase
{
    public override bool CanInteract
    {
        get
        {
            bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
            bool polylineHovered = !hover.HoveredPolyline.IsNull;
            return hasSelection || polylineHovered;
        }
    }

    private List<Entity> _polylineEs;

    public override void Prepare(CursorButtonData data)
    {
        bool hasSelection = SelectionManager.SelectedPolylines.Count > 0;
        bool polylineHovered = !hover.HoveredPolyline.IsNull;

        if (polylineHovered)
        {
            _polylineEs = [hover.HoveredPolyline];
        }
        else if (hasSelection)
        {
            _polylineEs = [..SelectionManager.SelectedPolylines];
            SelectionManager.SelectedPolylines.Clear();
        }
    }

    public override void Start(CursorButtonData data)
    {
        var cmd = new EmptyCommand();
        foreach (var e in _polylineEs)
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