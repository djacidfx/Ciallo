using Godot;

namespace Ciallo.Rendering;

public partial class PolylineLayerView : CanvasGroup
{
    public PolylineLayerView()
    {
    }

    // If can be replace by a regular node2D
    public bool IsDefault => true;
}