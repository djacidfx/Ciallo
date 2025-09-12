using Godot;
using System;

namespace Ciallo.Tool;

public partial class SelectTool : CommonToolBase
{
    public readonly StrokeSelectionHintInteractor HintInteractor = new();
    
    public override InteractorBase HoveringInteractor => HintInteractor;
}
