using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PaintHover : InteractiveSessionBase
{
    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = Control.CursorShape.Cross;
    }
    public override void Moving(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;

    public override void DrawProperty(PropertyContainer container)
    {
        var ppCurveEdit = new MappingCurveEdit();
        ppCurveEdit.Curve = AppPreference.PenPressureRemapCurve;
        var aspectBox = new AspectRatioContainer();
        aspectBox.AddChild(ppCurveEdit);
        container.AddProperty("Global pen pressure remap", aspectBox);

        var brushSelector = new OptionButton()
            {
                CustomMinimumSize = new(256, 32),
                FitToLongestItem = false,
            }
            .ObserveObservableList(AppBrushLibrary.BrushSettings, s => s.Name)
            .BindSelectionIndex(AppBrushLibrary.SelectedIndex);
        container.AddProperty("Library brush", brushSelector);

        var radiusView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.BaseRadius)
            .ToReadOnlyReactiveProperty();
        var appBrushRadiusControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        }.ReactiveBindNumber(radiusView);
        container.AddProperty("Radius", appBrushRadiusControl)
            .VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);

        var colorView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.Color)
            .ToReadOnlyReactiveProperty();
        var appBrushColorControl = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 32),
        }.ReactiveBindColor(colorView);
        container.AddProperty("Color", appBrushColorControl)
            .VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);

        var useBrushButton = new Button()
        {
            Text = "Use brush",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 32),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        useBrushButton.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);
        useBrushButton.Pressed += OnUseBrushPressed;
        var manageButton = new Button()
        {
            Text = "Manage brush library",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 32),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };

        manageButton.Pressed += () =>
        {
            var godotTree = (SceneTree)Engine.GetMainLoop();
            godotTree.GetNodesInGroup("Dialog").OfType<BrushPanel>().First().Popup();
        };

        var box = new VBoxContainer()
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        box.AddChild(manageButton);
        box.AddChild(useBrushButton);
        container.AddChild(box);
        // ---------------------------------------------
        container.AddChild(new HSeparator());
        // ---------------------------------------------
        var brushList = new DocumentBrushListViewer()
        {
            CustomMinimumSize = new(256, 150),
        };
        Document.Add(brushList);
        container.AddProperty("Brush in document", brushList);

        var selectionM = Document.Get<SelectionManager>();
        var rView = selectionM.WorkingBrush
            .Select(e => e.IsDyingOrDead ? null : e.Get<StrokeBrushSetting>().BaseRadius)
            .ToReadOnlyReactiveProperty();
        var radiusControl = new SpinSlider
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true,
        }.ReactiveBindNumber(rView);
        container.AddProperty("Radius", radiusControl)
            .VisibleIf(selectionM.WorkingBrush, e => !e.IsNull);

        var manageDocumentBrush = new Button()
        {
            Text = "Manage brush in document",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 32),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        manageDocumentBrush.Pressed += () => Document.Get<BrushPanel>().Popup();
        container.AddChild(manageDocumentBrush);
    }

    private void OnUseBrushPressed()
    {
        if (!AppBrushLibrary.HasSelection) return;
        var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
        new CommandBuilder(AppDocumentManager.WorkingDocument.Value.World.Create())
            .NewBrush(setting)
            .SetWorkingBrush()
            .Commit();
        AppBrushLibrary.SelectedIndex.Value = -1;
    }
}