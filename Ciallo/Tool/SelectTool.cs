using Godot;
using System;

namespace Ciallo.Tool;

public partial class SelectTool : ToolBaseSingularInteractor
{
    public readonly StrokeSelectionHintInteractor HintInteractor = new();
    
    public override InteractorBase LeftInteractor => HintInteractor;
}
