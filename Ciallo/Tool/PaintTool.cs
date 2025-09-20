using Arch.Core;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using Humanizer;
using R3;

public partial class PaintTool : CommonToolBase
{
    public readonly ReactiveProperty<float> BrushSize = new(8f);
    public readonly ReactiveProperty<Entity> BrushE = new(Entity.Null);
    
    public override InteractorBase LeftInteractor => PaintInteractor;
    
    public readonly PaintInteractor PaintInteractor;
    // Will have dual interactors
    // public readonly ResizeBrushInteractor ResizeInteractor = new();

    public PaintTool()
    {
        PaintInteractor = new()
        {
            ToolBrushSize = BrushSize
        };
    }

    public override void _Ready()
    {
        base._Ready();
        SetPressed(true);
    }

    public override void DrawProperty(PropertyContainer container)
    {
        var brushSizeControl = new SpinSlider()
        {
            MinValue = 0.1f,
            MaxValue = 256f,
            Step = 0.03333333f,
            ExpEdit = true
        };
        brushSizeControl.BindValue(BrushSize).AddTo(brushSizeControl);
        container.AddPropertyControl(nameof(BrushSize).Humanize(), brushSizeControl);

        var brushSelector = new OptionButton();
        var view = AppBrushLibrary.Brushes.CreateWritableView(setting => setting.Name);
        view.AddTo(brushSelector);
        brushSelector.BindValue(view, AppBrushLibrary.CurrentBrush).AddTo(brushSelector);
        container.AddPropertyControl("Brush".Tr(), brushSelector);
    }
}