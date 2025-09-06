using System;
using Ciallo.Data;
using Godot;

namespace Ciallo.Rendering;

public static class VectorLayerView
{
    /// <summary>
    /// Create a default layer view at runtime.
    /// </summary>
    public static CanvasGroup Create()
    {
        var layerView = new CanvasGroup();
        return layerView;
    }

    /// <summary>
    /// Create the best node type for the layer view and will be exported to Godot.
    /// </summary>
    public static Node2D CreateOptimized(VectorLayerSetting setting)
    {
        throw new NotImplementedException();
    }
}