using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

public class SetStrokeBrushCmd(Entity strokeE, Entity newBrushE) : CommandBase
{
    private Entity _oldBrushE;

    public override void Do()
    {
        // Data
        var wrapper = strokeE.Get<StrokeBrush>();
        _oldBrushE = wrapper.Value;
        wrapper.Value = newBrushE;

        // View
        strokeE.Get<StrokeView>().Material = !newBrushE.IsNull ? newBrushE.Get<BrushMaterial>() : AutoloadRendering.MissingBrushMaterial;
    }

    public override void Undo()
    {
        // View
        strokeE.Get<StrokeView>().Material = !_oldBrushE.IsNull ? _oldBrushE.Get<BrushMaterial>() : AutoloadRendering.MissingBrushMaterial;

        // Data
        var wrapper = strokeE.Get<StrokeBrush>();
        wrapper.Value = _oldBrushE;
    }
}