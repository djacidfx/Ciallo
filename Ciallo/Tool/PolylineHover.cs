using System;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PolylineHover : HoverBase
{
    public Entity HoveredPolyline;

    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return !layerE.IsNull && layerE.Has<PolylineLayerSetting>();
        }
    }

    private IDisposable _hoverSub;
    private Entity _layerE;
    public override void Start(CursorMotionData data)
    {
        _layerE = SelectionManager.WorkingLayer.Value;

        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Inherit;

        _hoverSub = Document.Get<WorldCursorDetectionArea>().HoveringArea.Skip(1).Subscribe(area =>
        {
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(false);
            if (area == null)
            {
                HoveredPolyline = Entity.Null;
                return;
            }
            HoveredPolyline = area.SelfEntity;
            if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(true);
        });
    }

    public override void Cancel()
    {
        _hoverSub.Dispose();
        if (!HoveredPolyline.IsNull) HoveredPolyline.Get<StrokeOverlay>().SetVisible(false);

        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}