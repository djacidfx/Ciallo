using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Arch.Core.Extensions;
using Arch.Core;
using Ciallo.Command;
using Ciallo.Rendering;

namespace Ciallo.Data;

public class ResetStrokeGeometryCmd(IReadOnlyList<int> targetPath, IReadOnlyList<Vector2> newPoints, IReadOnlyList<float> newRadii) : CommandBase
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
        targetE.Get<StrokeView>().UpdateStroke(_newGeometry.Points, _newGeometry.Radii);
    }

    public override void Undo()
    {
        var tree = Document.Get<LayerTreeManager>();
        var targetE = tree.Root.GetDescendant(_targetPath);
        
        // Data
        targetE.Set(_oldGeometry);
        
        // View
        targetE.Get<StrokeView>().UpdateStroke(_oldGeometry.Points, _oldGeometry.Radii);
    }
}