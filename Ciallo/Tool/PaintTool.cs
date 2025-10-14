using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using Massive;
using R3;

public partial class PaintTool : CommonToolBase
{
    public readonly ReactiveProperty<Entity> BrushE = new(new Entity());

    public readonly PaintInteractor PaintInteractor = new();
    public readonly PaintHover PaintHover = new();

    public override InteractorBase LeftInteractor => PaintInteractor;
    public override HoverBase HoveringInteractor => PaintHover;

    // Will have dual interactors
    // public readonly ResizeBrushInteractor ResizeInteractor = new();

    public override void DrawProperty(PropertyContainer container)
    {
        var brushSelector = new OptionButton();
        brushSelector.ObserveObservableList(AppBrushLibrary.BrushSettings, s => s.Name);
        brushSelector.BindSelectionIndex(AppBrushLibrary.SelectedIndex);
        container.AddProperty("Library brush", brushSelector);

        var appBrushRadiusControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        };
        var boxBrushRadius = container.AddProperty("Radius", appBrushRadiusControl);
        boxBrushRadius.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);
        var radiusView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.BaseRadius)
            .ToReadOnlyReactiveProperty();
        appBrushRadiusControl.ReactiveBindNumber(radiusView);

        var appBrushColorControl = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 30),
        };
        var boxBrushColor = container.AddProperty("Color", appBrushColorControl);
        boxBrushColor.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);
        var colorView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.Color).ToReadOnlyReactiveProperty();
        appBrushColorControl.ReactiveBindColor(colorView);

        var useBrushButton = new Button()
        {
            Text = "Use brush",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        useBrushButton.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);
        useBrushButton.Pressed += OnUseBrushPressed;
        var manageButton = new Button()
        {
            Text = "Manage brush library",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        manageButton.Pressed += () => GetTree().GetNodesInGroup("Dialog").OfType<BrushPanel>().First().Popup();
        var box = new VBoxContainer()
        {
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        box.AddChild(manageButton);
        box.AddChild(useBrushButton);
        container.AddChild(box);
        // ---------------------------------------------
        container.AddChild(new HSeparator());
        // ---------------------------------------------
        var brushList = new DocumentBrushList()
        {
            CustomMinimumSize = new(0, 200),
        };
        brushList.ItemSelected += idx => { new ChangeWorkingBrushCmd((int)idx).Commit(); };
        var brushM = Document.Get<BrushManager>();
        var selectionM = Document.Get<SelectionManager>();
        foreach (var brushE in brushM.Brushes)
            brushList.AddItem(brushE.Get<BrushSetting>().Name.Value);
        Document.Set(brushList);
        container.AddProperty("Brush in document", brushList);

        var radiusControl = new SpinSlider
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true,
        };
        var radiusBox = container.AddProperty("Radius", radiusControl);
        radiusBox.VisibleIf(selectionM.WorkingBrush, e => e.IsNotNull());
        var rView = selectionM.WorkingBrush
            .Select(e => e.IsNull() ? null : e.Get<BrushSetting>().BaseRadius).ToReadOnlyReactiveProperty();
        radiusControl.ReactiveBindNumber(rView);

        var manageDocumentBrush = new Button()
        {
            Text = "Manage brush in document",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        manageDocumentBrush.Pressed += () => Document.Get<BrushPanel>().Popup();
        container.AddChild(manageDocumentBrush);
    }

    public override void OnActivate()
    {
    }

    public override void OnDeactivate()
    {
    }

    private void OnUseBrushPressed()
    {
        if (!AppBrushLibrary.HasSelection) return;
        var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
        new NewBrushCmd(setting).Combine(new ChangeWorkingBrushCmd(^1)).Commit();
    }
}

public partial class DocumentBrushList : ItemList;

public class PaintHover : HoverBase
{
    public override bool CanInteract
    {
        get
        {
            var l = SelectionManager.WorkingLayer.Value;
            return l.IsNotNull() && l.Has<PolylineLayerSetting>();
        }
    }

    public override void Interacting(CursorMotionData data)
    {
        Input.SetDefaultCursorShape(Input.CursorShape.Cross);
    }

    public override void Cancel()
    {
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }
}