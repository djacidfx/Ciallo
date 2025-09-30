using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.Rendering;

namespace Ciallo.Data;

public class SetStrokeGeometryCmd(IReadOnlyList<int> targetPath, IReadOnlyList<Vector2> newPoints, IReadOnlyList<float> newRadii) : CommandBase
{
    private readonly ImmutableArray<int> _targetPath = [..targetPath];
    private readonly StrokeGeometry _newGeometry = new()
    {
        Points = [..newPoints],
        Radii = [..newRadii],
    };
    private StrokeGeometry _oldGeometry;
    
    public override void Do()
    {
        var tree = Document.Get<LayerTreeManager>();
        var targetE = tree.Root.GetDescendant(_targetPath);
        
        // Data
        _oldGeometry ??= targetE.Get<StrokeGeometry>();
        targetE.Set(_newGeometry);
        
        // View
        targetE.Get<StrokeView>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);
        
        // Overlay
        targetE.Get<StrokeOverlay>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);
    }

    public override void Undo()
    {
        var tree = Document.Get<LayerTreeManager>();
        var targetE = tree.Root.GetDescendant(_targetPath);
        
        // Overlay
        targetE.Get<StrokeOverlay>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // View
        targetE.Get<StrokeView>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);
        
        // Data
        targetE.Set(_oldGeometry);
    }
}