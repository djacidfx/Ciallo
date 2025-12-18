using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.GuiBinding;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Tool;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

public partial class PaintTool : CommonToolBase
{
    public readonly ReactiveProperty<Entity> BrushE = new(new Entity());

    public PaintTool()
    {
        LeftInteractor = new PaintInteractor();
        HoverInteractor = new PaintHover();
    }

    // Will have dual interactors
    // public readonly ResizeBrushInteractor ResizeInteractor = new();

    public override void DrawProperty(PropertyContainer container)
    {
        var ppCurveEdit = new MappingCurveEdit();
        ppCurveEdit.Curve = AppPreference.PenPressureRemapCurve;
        var aspectBox = new AspectRatioContainer();
        aspectBox.AddChild(ppCurveEdit);
        container.AddProperty("Global pen pressure remap", aspectBox);

        var brushSelector = new OptionButton()
            {
                CustomMinimumSize = new(0, 30),
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
        var boxBrushRadius = container.AddProperty("Radius", appBrushRadiusControl);
        boxBrushRadius.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);

        var colorView = AppBrushLibrary.SelectedIndex
            .Select(idx => AppBrushLibrary.BrushSettings.ElementAtOrDefault(idx)?.Color)
            .ToReadOnlyReactiveProperty();
        var appBrushColorControl = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 30),
        }.ReactiveBindColor(colorView);
        var boxBrushColor = container.AddProperty("Color", appBrushColorControl);
        boxBrushColor.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);

        var useBrushButton = new Button()
        {
            Text = "Use brush",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        useBrushButton.VisibleIf(AppBrushLibrary.SelectedIndex, v => v >= 0);
        useBrushButton.Pressed += OnUseBrushPressed;
        var manageButton = new Button()
        {
            Text = "Manage brush library",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        manageButton.Pressed += () => GetTree().GetNodesInGroup("Dialog").OfType<BrushPanel>().First().Popup();
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
        var brushList = new DocumentBrushList()
        {
            CustomMinimumSize = new(0, 150),
        };
        brushList.ItemSelected += idx =>
        {
            new CommandBuilder(Document.Get<BrushManager>().Brushes[(int)idx]).SetWorkingBrush().Commit();
        };
        brushList.ItemClicked += async (idx, _, buttonIndex) =>
        {
            if ((MouseButton)buttonIndex != MouseButton.Right) return;
            var brushE = Document.Get<BrushManager>().Brushes[(int)idx];
            var query = brushE.World.CreateQuery().With<StrokeSetting>().Build();
            List<Entity> toDeleteStrokes = [];
            foreach (var strokeE in query.EnumerateWithEntities())
            {
                if (strokeE.Get<StrokeSetting>().BrushE == brushE)
                    toDeleteStrokes.Add(strokeE);
            }

            if (toDeleteStrokes.Count > 0)
            {
                var dialog = GetTree().GetNodesInGroup("Dialog").OfType<YesNoDialog>().First();
                dialog.DialogText = "[Delete Brush Hint]".Tr();
                if (!await dialog.PopupCollectInput()) return;
            }

            var builder = new CommandBuilder(Entity.Null);
            foreach (var strokeE in toDeleteStrokes)
            {
                builder.SetTarget(strokeE).DeleteStroke();
            }

            var selectionManager = Document.Get<SelectionManager>();
            if (selectionManager.WorkingBrush.Value == brushE)
                builder.SetTarget(Entity.Null).SetWorkingBrush();
            builder.SetTarget(brushE).DeleteBrush();
            builder.Commit();
        };
        var brushM = Document.Get<BrushManager>();
        var selectionM = Document.Get<SelectionManager>();
        foreach (var brushE in brushM.Brushes)
            brushList.AddItem(brushE.Get<BrushSetting>().Name.Value);
        Document.Add(brushList);
        container.AddProperty("Brush in document", brushList);

        var rView = selectionM.WorkingBrush
            .Select(e => e.IsDeletedOrNull() ? null : e.Get<BrushSetting>().BaseRadius).ToReadOnlyReactiveProperty();
        var radiusControl = new SpinSlider
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true,
        }.ReactiveBindNumber(rView);
        var radiusBox = container.AddProperty("Radius", radiusControl);
        radiusBox.VisibleIf(selectionM.WorkingBrush, e => !e.IsNull);

        var manageDocumentBrush = new Button()
        {
            Text = "Manage brush in document",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        manageDocumentBrush.Pressed += () => Document.Get<BrushPanel>().Popup();
        container.AddChild(manageDocumentBrush);
    }

    private void OnUseBrushPressed()
    {
        if (!AppBrushLibrary.HasSelection) return;
        var setting = AppBrushLibrary.SelectedBrushSetting.CurrentValue;
        new CommandBuilder(AppWorldManager.WorkingWorld.Value.Create())
            .NewBrush(setting)
            .SetWorkingBrush()
            .Commit();
    }

    public override bool CanHandleLayer(ICollection<Entity> layerEs)
    {
        if (layerEs.Count != 1) return false;
        var e = layerEs.Single();
        return !e.IsDeletedOrNull() && e.Has<PolylineLayerSetting>();
    }
}