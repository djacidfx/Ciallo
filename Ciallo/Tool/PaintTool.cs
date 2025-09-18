using Ciallo.Misc;
using Ciallo.Tool;
using Ciallo.Widget;
using Humanizer;
using R3;

public partial class PaintTool : CommonToolBase
{
    public ReactiveProperty<float> BrushSize { get; } = new(8f);
    
    public override InteractorBase LeftInteractor => PaintInteractor;
    
    public readonly PaintInteractor PaintInteractor = new();
    // Will have dual interactors
    // public readonly ResizeBrushInteractor BrushInteractor = new();

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
    }

    public override void _Ready()
    {
        base._Ready();
        SetPressed(true);
    }
}