using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.Select)]
public class PolylineSelectTool : StateMachineToolBase
{
    public readonly PolylineSelectHover Hover = new();
    public readonly PolylineTransformInteractor Transform = new();
    public readonly RectSelectPolylineInteractor Select = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitDynamic(Press(MouseButton.Left), () =>
            {
                if (Hover.CanTransform)
                    return Transform;
                return Select;
            });

        Configure(Transform)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);

        Configure(Select)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        bool isShapeLayer = e.Has<ShapeLayerSetting>();
        bool isVectorFillLayer = e.Has<VectorFillLayerSetting>();
        return isShapeLayer || isVectorFillLayer;
    }

    public override void OnActivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = true;
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Inherit;
        // Guard selection
        var selectedShapes = Document.Get<SelectionManager>().SelectedShapes;
        var deselect = selectedShapes
            .Where(e => e.Get<LayerTreeNode>().ParentValue != WorkingLayer).Reverse().ToArray();
        foreach (var e in deselect)
            selectedShapes.Remove(e);
    }

    public override void OnDeactivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = false;
        WorkingLayer.Get<BodyHolder>().ProcessMode = Node.ProcessModeEnum.Disabled;
    }

    public override void DrawProperty(PropertyContainer container)
    {
        // Session properties
        base.DrawProperty(container);

        var selectionManager = Document.Get<SelectionManager>();
        var selectionButtonGroup = container.CreateHContainer().AddToChildOf(container);
        var selectAllButton = container.CreateButton("Select all").AddToChildOf(selectionButtonGroup);
        selectAllButton.Pressed += () =>
        {
            var layerE = selectionManager.WorkingLayer.Value;
            if (layerE.IsDyingOrDead) return;
            selectionManager.SelectedShapes.Clear();
            selectionManager.SelectedShapes.AddRange(layerE.Get<LayerTreeNode>().Children);
            ActiveSession.CurrentValue.Refresh();
        };
        var deselectAllButton = container.CreateButton("Deselect").AddToChildOf(selectionButtonGroup);
        deselectAllButton.Pressed += () =>
        {
            selectionManager.SelectedShapes.Clear();
            ActiveSession.CurrentValue.Refresh();
        };

        var selectedShapes = Document.Get<SelectionManager>().SelectedShapes;
        var selectionChanged = selectedShapes.ObserveChanged().Select(_ => Unit.Default).Prepend(Unit.Default);

        // --- Stroke brush switcher
        var strokeBrushSwitcher = StrokeBrushPreviewList.New().AddToChildOf(container);
        strokeBrushSwitcher.CustomMinimumSize = new(0, 256);
        strokeBrushSwitcher.Document = Document;
        strokeBrushSwitcher.BindBrushes(Document.Get<BrushManager>().StrokeBrushEs);
        strokeBrushSwitcher.VisibleIf(selectionChanged,
            _ => selectedShapes.Count > 0 && selectedShapes.All(e => e.Has<StrokeSetting>()));

        selectionChanged.Subscribe(_ =>
        {
            if (selectedShapes.Count <= 0 || !selectedShapes.All(e => e.Has<StrokeSetting>())) return;
            var firstE = selectedShapes.First().Get<StrokeSetting>().BrushE.Value;
            bool allSame = selectedShapes.All(e => e.Get<StrokeSetting>().BrushE.Value == firstE);
            strokeBrushSwitcher.Select(allSame ? firstE : Entity.Null);
        }).AddTo(strokeBrushSwitcher);

        strokeBrushSwitcher.BrushClicked.Subscribe(brushE =>
        {
            var cmd = new CommandBuilder();
            foreach (var shapeE in selectedShapes)
                cmd.SetTarget(shapeE).SetProperty(e => e.Get<StrokeSetting>().BrushE, brushE);
            cmd.Commit();
            strokeBrushSwitcher.Select(brushE);
        }).AddTo(strokeBrushSwitcher);

        // --- Vector fill brush switcher
        var vectorFillBrushSwitcher = VectorFillBrushPreviewList.New().AddToChildOf(container);
        vectorFillBrushSwitcher.CustomMinimumSize = new(0, 256);
        vectorFillBrushSwitcher.Document = Document;
        vectorFillBrushSwitcher.BindBrushes(Document.Get<BrushManager>().VectorFillBrushEs);
        vectorFillBrushSwitcher.VisibleIf(selectionChanged,
            _ => selectedShapes.Count > 0 && selectedShapes.All(e => e.Has<VectorFillMarkerSetting>() || e.Has<FilledPolygonSetting>()));

        selectionChanged.Subscribe(_ =>
        {
            if (selectedShapes.Count <= 0 || !selectedShapes.All(e => e.Has<VectorFillMarkerSetting>() || e.Has<FilledPolygonSetting>())) return;
            var firstE = GetVectorFillBrushE(selectedShapes.First()).Value;
            bool allSame = selectedShapes.All(e => GetVectorFillBrushE(e).Value == firstE);
            vectorFillBrushSwitcher.Select(allSame ? firstE : Entity.Null);
        }).AddTo(vectorFillBrushSwitcher);

        vectorFillBrushSwitcher.BrushClicked.Subscribe(brushE =>
        {
            var cmd = new CommandBuilder();
            foreach (var shapeE in selectedShapes)
            {
                if (shapeE.Has<VectorFillMarkerSetting>())
                    cmd.SetTarget(shapeE).SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, brushE);
                else
                    cmd.SetTarget(shapeE).SetProperty(e => e.Get<FilledPolygonSetting>().BrushE, brushE);
            }
            cmd.Commit();
            vectorFillBrushSwitcher.Select(brushE);
        }).AddTo(vectorFillBrushSwitcher);
    }

    private static ReactiveProperty<Entity> GetVectorFillBrushE(Entity e)
    {
        if (e.Has<VectorFillMarkerSetting>()) return e.Get<VectorFillMarkerSetting>().BrushE;
        return e.Get<FilledPolygonSetting>().BrushE;
    }
}