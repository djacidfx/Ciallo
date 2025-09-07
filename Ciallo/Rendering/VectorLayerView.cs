using System;
using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public partial class VectorLayerView : CanvasGroup
{
    public VectorLayerView()
    {
    }

    /// <summary>
    /// Create the best node type for the layer view and will be exported to Godot.
    /// </summary>
    public static Node2D CreateOptimized(VectorLayerSetting setting)
    {
        throw new NotImplementedException();
    }
}