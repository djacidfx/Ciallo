using System;
using Ciallo.Command;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PolylineSelectionHintHover : HoverBase
{
    private StrokeOverlay _hintingOverlay;

    public override bool CanInteract => SelectionManager.WorkingLayer.Value.IsNotNull();

    private IDisposable _hoverSub;
    private Entity _layerE;
    public override void Start(CursorMotionData data)
    {
        _layerE = SelectionManager.WorkingLayer.Value;
        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Inherit;

        _hoverSub = Document.Get<WorldCursorDetectionArea>().HoveringArea.Skip(1).Subscribe(area =>
        {
            _hintingOverlay?.SetVisible(false);
            if (area == null)
            {
                _hintingOverlay = null;
                return;
            }
            _hintingOverlay = area.SelfEntity.Get<StrokeOverlay>();
            _hintingOverlay.SetVisible(true);
        });
    }

    public override void Cancel()
    {
        _hoverSub.Dispose();
        _hintingOverlay?.SetVisible(false);
        _hintingOverlay = null;

        _layerE.Get<PolylineAreaHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}