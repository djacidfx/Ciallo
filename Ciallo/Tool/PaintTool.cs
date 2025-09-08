using Ciallo.Tool;

public partial class PaintTool : ToolBaseSingularInteractor
{
    public override InteractorBase LeftInteractor => PaintInteractor;
    public readonly PaintInteractor PaintInteractor = new();
    // Will have dual interactors
    // public readonly ResizeBrushInteractor BrushInteractor = new();

    public override void _Ready()
    {
        base._Ready();
        SetPressed(true);
    }
}
