using System;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Paint)]
public class PaintStrokeTool : StateMachineToolBase
{
    public readonly ReactiveProperty<Entity> BrushE = new(Entity.Null);

    public readonly PaintStrokeHover Hover = new();
    public readonly PaintStrokeInteractor Left = new();
    public readonly PaintStrokeOnVectorFill LeftOnFill = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitDynamicIf(Press(MouseButton.Left), () =>
            {
                if (WorkingLayer.Has<ShapeLayerSetting>())
                    return Left;
                if (WorkingLayer.Has<VectorFillLayerSetting>())
                    return LeftOnFill;
                throw new InvalidOperationException("Unreachable code: layer type is guaranteed by CanHandleLayer");
            }, () =>
            {
                var brushE = Document.Get<SelectionManager>().WorkingStrokeBrush.Value;
                return !brushE.IsDyingOrDead || AppStrokeBrushLibrary.HasSelection;
            });

        Configure(Left)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(PaintStrokeInteractor.PaintEnd, Hover);

        Configure(LeftOnFill)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(PaintStrokeInteractor.PaintEnd, Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        bool isShapeLayer = e.Has<ShapeLayerSetting>();
        bool isVectorFillLayer = e.Has<VectorFillLayerSetting>();
        return isShapeLayer || isVectorFillLayer;
    }

    public readonly Subject<Unit> DeactivateSignal = new();
    public override void OnActivated()
    {
        if (!WorkingLayer.Has<VectorFillLayerSetting>()) return;

        var referenceLayers = WorkingLayer.Get<VectorFillLayerSetting>().ReferenceLayers;
        AppPreference.ShowVectorFillReferenceLayerWireframe
            .TakeUntil(DeactivateSignal)
            .Subscribe(visible => VectorFillTool.SetWireframeVisibility(referenceLayers, visible),
                _ => VectorFillTool.SetWireframeVisibility(referenceLayers, false));
    }

    public override void OnDeactivated()
    {
        DeactivateSignal.OnNext(Unit.Default);
    }
}