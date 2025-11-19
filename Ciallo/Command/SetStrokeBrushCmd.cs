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
        var setting = strokeE.Get<StrokeSetting>();
        _oldBrushE = setting.BrushE;
        setting.BrushE = newBrushE;

        // View
        strokeE.Get<StrokeView>().Material = !newBrushE.IsNull ? newBrushE.Get<BrushMaterial>() : AutoloadRendering.MissingBrushMaterial;
    }

    public override void Undo()
    {
        // View
        strokeE.Get<StrokeView>().Material = !_oldBrushE.IsNull ? _oldBrushE.Get<BrushMaterial>() : AutoloadRendering.MissingBrushMaterial;

        // Data
        var setting = strokeE.Get<StrokeSetting>();
        setting.BrushE = _oldBrushE;
    }
}