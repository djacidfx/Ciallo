using System;
using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeLayerView : CanvasGroup
{
    public StrokeLayerView()
    {
    }

    /// <summary>
    /// Create the best node type for the layer view and will be exported to Godot.
    /// </summary>
    public static Node2D CreateOptimized(StrokeLayerSetting setting)
    {
        throw new NotImplementedException();
    }
}