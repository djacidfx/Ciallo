using System.Collections.Specialized;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using Humanizer;
using ObservableCollections;
using R3;

public partial class PaintTool : CommonToolBase
{
    public readonly ReactiveProperty<Entity> BrushE = new(Entity.Null);
    
    public override InteractorBase LeftInteractor => PaintInteractor;
    
    public readonly PaintInteractor PaintInteractor = new();
    // Will have dual interactors
    // public readonly ResizeBrushInteractor ResizeInteractor = new();

    public override void _Ready()
    {
        base._Ready();
        SetPressed(true);
    }

    public override void DrawProperty(PropertyContainer container, Entity document)
    {
        var brushSelector = new OptionButton();
        var view = AppBrushLibrary.Brushes.CreateWritableView(setting => setting.Name);
        view.AddTo(brushSelector);
        brushSelector.BindValue(view, AppBrushLibrary.CurrentBrush);
        container.AddProperty("Library brush", brushSelector);
        
        var appBrushRadiusControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        };
        var boxBrushRadius = container.AddProperty("Radius", appBrushRadiusControl);
        boxBrushRadius.VisibleIf(AppBrushLibrary.CurrentBrush, v => v != null);
        var radiusView = AppBrushLibrary.CurrentBrush
            .Select(setting => setting?.BaseRadius).ToReadOnlyReactiveProperty();
        appBrushRadiusControl.ReactiveBindNumber(radiusView);
        
        var appBrushColorControl = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 30),
        };
        var boxBrushColor = container.AddProperty("Color", appBrushColorControl);
        boxBrushColor.VisibleIf(AppBrushLibrary.CurrentBrush, v => v != null);
        var colorView = AppBrushLibrary.CurrentBrush.Select(setting => setting?.Color).ToReadOnlyReactiveProperty();
        appBrushColorControl.ReactiveBindColor(colorView);
        
        var useBrushButton = new Button()
        {
            Text = "Use brush",
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        useBrushButton.VisibleIf(AppBrushLibrary.CurrentBrush, v => v != null);
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
        var brushList = new DocumentBrushList()
        {
            CustomMinimumSize = new(0, 200),
        };
        brushList.ItemSelected += idx =>
        {
            new ChangeWorkingBrushCmd((int)idx).Commit();
        };
        var brushM = document.Get<BrushManager>();
        var selectionM = document.Get<SelectionManager>();
        foreach(var brush in brushM.Brushes)
            brushList.AddItem(brush.Get<BrushSetting>().Name.Value);
        document.Add(brushList);
        container.AddProperty("Brush in document", brushList);
        
        var radiusControl = new SpinSlider
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true,
        };
        var radiusBox = container.AddProperty("Radius", radiusControl);
        radiusBox.VisibleIf(selectionM.SelectedBrush, e => e != Entity.Null);
        var rView = selectionM.SelectedBrush
            .Select(e => e == Entity.Null ? null : e.Get<BrushSetting>().BaseRadius).ToReadOnlyReactiveProperty();
        radiusControl.ReactiveBindNumber(rView);
    }

    private void OnUseBrushPressed()
    {
        var setting = AppBrushLibrary.CurrentBrush.Value;
        if (setting == null) return;
        AppBrushLibrary.CurrentBrush.Value = null;
        new NewBrushCmd(setting).Commit();
    }
}

public partial class DocumentBrushList : ItemList;