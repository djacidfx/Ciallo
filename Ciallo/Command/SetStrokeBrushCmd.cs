using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetStrokeBrushCmd : CommandBase
{
    private Entity _oldBrushE;
    private readonly Entity _newBrushE;

    public SetStrokeBrushCmd(Entity newBrushE)
    {
        _newBrushE = newBrushE;
    }

    public override void Do(Entity strokeE)
    {
        // Data
        var setting = strokeE.Get<StrokeSetting>();
        _oldBrushE = setting.BrushE;
        setting.BrushE = _newBrushE;

        // View
        strokeE.Get<StrokeView>().Material =
            _newBrushE.IsNull ? AutoloadRendering.MissingBrushMaterial : _newBrushE.Get<BrushMaterial>();
    }

    public override void Undo(Entity strokeE)
    {
        // View
        strokeE.Get<StrokeView>().Material = !_oldBrushE.IsNull
            ? _oldBrushE.Get<BrushMaterial>()
            : AutoloadRendering.MissingBrushMaterial;

        // Data
        var setting = strokeE.Get<StrokeSetting>();
        setting.BrushE = _oldBrushE;
    }
}