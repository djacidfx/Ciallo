using System.Collections.Specialized;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
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
        brushSelector.BindValue(view, AppBrushLibrary.CurrentBrush).AddTo(brushSelector);
        container.AddProperty("Library brush", brushSelector);
        
        var manageButton = new Button()
        {
            Text = "Manage brush library",
            CustomMinimumSize = new(0, 30),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        manageButton.Pressed += () => GetTree().GetNodesInGroup("Dialog").OfType<AppBrushLibrary>().First().Popup();
        container.AddChild(manageButton);
        
        var appBrushRadiusControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        };
        var box = container.AddProperty("Size", appBrushRadiusControl);
        
        CompositeDisposable subs = new();
        AppBrushLibrary.CurrentBrush.Subscribe(setting =>
        {
            if (setting == null)
            {
                box.Visible = false;
                subs?.Dispose();
                subs = null;
            }
            else
            {
                box.Visible = true;
                subs?.Dispose();
                subs = appBrushRadiusControl.BindValue(setting.BaseRadius);
            }
        }).AddTo(container);
        
    }
}