using System;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PaintStrokeHover : InteractiveSessionBase
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
        var ppCurveEdit = new MappingCurveEdit().BindCurve(AppPreference.PenPressureRemapCurve);
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
            .Flatten();
        var appBrushRadiusControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        }.BindNumber(radiusView);
        radiusView.AddTo(appBrushRadiusControl);

        container.AddProperty("Radius", appBrushRadiusControl)
            .VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);

        var colorView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.Color)
            .Flatten();
        var appBrushColorControl = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 32),
        }.BindColor(colorView);
        colorView.AddTo(appBrushColorControl);
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
        var brushPreview = StrokeBrushPreviewList.New(Document);
        brushPreview.CustomMinimumSize = new(0, 256);
        container.AddChild(brushPreview);

        var selectionM = Document.Get<SelectionManager>();
        var radius = selectionM.WorkingStrokeBrush
            .Select(e => e.IsDyingOrDead ? null : e.Get<StrokeBrushSetting>().BaseRadius)
            .Flatten();
        var radiusControl = new SpinSlider
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true,
        }.BindNumber(radius);
        radius.AddTo(radiusControl);
        container.AddProperty("Radius", radiusControl)
            .VisibleIf(selectionM.WorkingStrokeBrush, Entity.IsNotNull);

        var taperTime = AppPreference.TaperDuration.Project(
            time => time.TotalMilliseconds, TimeSpan.FromMilliseconds);
        container.AddProperty("Taper end", new SpinSlider()
            {
                MinValue = 0f,
                MaxValue = 50f,
            }.BindNumber(taperTime))
            .VisibleIf(selectionM.WorkingStrokeBrush, Entity.IsNotNull);
        ;
    }

    private void OnUseBrushPressed()
    {
        if (!AppBrushLibrary.HasSelection) return;
        var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
        new CommandBuilder(Document.World.Create())
            .NewStrokeBrush(setting)
            .SetWorkingStrokeBrush()
            .Commit();
        AppBrushLibrary.SelectedIndex.Value = -1;
    }
}