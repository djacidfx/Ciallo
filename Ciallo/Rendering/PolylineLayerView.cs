using System;
using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public partial class PolylineLayerView : CanvasGroup
{
    public PolylineLayerView()
    {
    }

    /// <summary>
    /// Create the best node type for the layer view and will be exported to Godot.
    /// </summary>
    public static Node2D CreateOptimized(PolylineLayerSetting setting)
    {
        throw new NotImplementedException();
    }
}