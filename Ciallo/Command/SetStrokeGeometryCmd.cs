using System.Collections.Generic;
using Godot;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Rendering;

namespace Ciallo.Data;

public class SetStrokeGeometryCmd(Entity strokeE, IReadOnlyList<Vector2> newPoints, IReadOnlyList<float> newRadii) : CommandBase
{
    private readonly StrokeGeometry _newGeometry = new()
    {
        Points = [..newPoints],
        Radii = [..newRadii],
    };
    private StrokeGeometry _oldGeometry;
    
    public override void Do()
    {
        // Data
        _oldGeometry ??= strokeE.Get<StrokeGeometry>();
        strokeE.Set(_newGeometry);
        
        // View
        strokeE.Get<StrokeView>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);
        
        // Overlay
        strokeE.Get<StrokeOverlay>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);
    }

    public override void Undo()
    {
        // Overlay
        strokeE.Get<StrokeOverlay>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // View
        strokeE.Get<StrokeView>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);
        
        // Data
        strokeE.Set(_oldGeometry);
    }
}