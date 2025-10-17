using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformInteractor(PolylineHover hover) : InteractorBase
{
    public override bool CanInteract => Document.Get<WorldCursorDetectionArea>().HoveringArea.CurrentValue != null;
    public int TransformType = -1; // 0: Move, 1: Rotate, 2~5: Scale corners
    private Entity _polylineE;

    public override void Start(CursorButtonData data)
    {
        if (!hover.HoveredPolyline.IsNull)
        {
            TransformType = 0;
            _polylineE = hover.HoveredPolyline;
        }
    }
    public override void Interacting(CursorMotionData data)
    {
        if (TransformType == 0 && _polylineE.Has<StrokeView>())
            _polylineE.Get<StrokeView>().Translate(data.WorldDelta);
    }

    public override void End(CursorButtonData data)
    {
        if (TransformType == 0 && _polylineE.Has<StrokeView>())
        {
            var strokeView = _polylineE.Get<StrokeView>();
            var transform = strokeView.GetTransform();
            strokeView.SetTransform(Transform2D.Identity);

            var newGeom = _polylineE.Get<StrokeGeometry>().Clone();
            newGeom.Points = newGeom.Points.Select(p => transform * p).ToList();
            new SetStrokeGeometryCmd(_polylineE, newGeom).Commit();
        }

        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        TransformType = -1;
        _polylineE = Entity.Null;
    }
}